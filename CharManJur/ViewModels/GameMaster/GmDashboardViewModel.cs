using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.ViewModels;

// One connected player as the GM sees them. Stats is worked out by the character service the way the live game shows it.
// PlayerId is the id the player joined the session with. IsConnected turns false when their link drops
// (they stay on the roster and can rejoin) and the Players panel disables the GM actions until they are back.
public class GmPlayerRow : INotifyPropertyChanged
{
    private bool _isConnected;

    public GmPlayerRow(CharacterSaveData save, GmPlayerSummary summary, Guid playerId = default, bool isConnected = true)
    {
        Save = save;
        Summary = summary;
        PlayerId = playerId;
        _isConnected = isConnected;
    }

    public CharacterSaveData Save { get; }
    public GmPlayerSummary Summary { get; }
    public Guid PlayerId { get; }

    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (_isConnected == value) return;
            _isConnected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsDisconnected));
        }
    }

    public bool IsDisconnected => !_isConnected;

    public string Title => $"{Save.CharacterName} ({Save.PlayerName})";

    public string Stats => Summary.Compact;                    // the one-line version (headline strip, Bestow and Viewer lists)
    public List<GmStatChip> Chips => Summary.Chips;            // every stat, for the Players panel
    public bool HasWaiting => Summary.Waiting > 0;
    public string WaitingText => $"📨 {Summary.Waiting} waiting";

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class GmDashboardViewModel : INotifyPropertyChanged
{
    // Test connections only: how many of a character's latest events are copied into the GM log when it is added.
    private const int HistoryImportCount = 25;

    private readonly ICampaignStore _store;
    private readonly ICharacterPersistenceService _persistence;
    private readonly IPlayerActionLogService _log;
    private readonly IGmEventLog _gmLog;
    private readonly IGmCharacterService _characters;
    private readonly IGmSessionHub _net;

    private Campaign? _campaign;
    private Guid _runId = Guid.NewGuid();   // one per opening of the dashboard and per opening of the session; never saved
    private bool _isOpen;
    private bool _inCombat;
    private readonly HashSet<Guid> _localJoinIds = new();   // players that live inside this app (test players), so they aren't listed as network players

    public GmDashboardViewModel(
        ICampaignStore store,
        ICharacterPersistenceService persistence,
        IPlayerActionLogService log,
        IGmEventLog gmLog,
        GmEventLogViewModel eventLog,
        GmBestowViewModel bestow,
        GmPlayersViewModel playerInterface,
        IGmCharacterService characters,
        GmViewerViewModel viewer,
        GmMapViewModel map,
        IGmSessionHub net)
    {
        _store = store;
        _persistence = persistence;
        _log = log;
        _gmLog = gmLog;
        _characters = characters;
        _net = net;
        EventLog = eventLog;

        Bestow = bestow;
        Bestow.Attach(Players, () => _runId, RefreshPlayersAsync, () => _campaign?.Id);

        PlayerInterface = playerInterface;
        PlayerInterface.Attach(Players, () => _campaign, () => _runId, LogAsync, RefreshPlayersAsync, RemovePlayer,
            () => _isOpen, ToggleSessionAsync);

        Viewer = viewer;
        Viewer.Attach(Players);

        Map = map;

        BeginCombatCommand = new Command(async () => await BeginCombatAsync());
        EndCombatCommand = new Command(async () => await EndCombatAsync());
        PassDayCommand = new Command(async () => await PassDayAsync());
        AddTestPlayerCommand = new Command(async () => await AddTestPlayerAsync());
        RemovePlayerCommand = new Command<GmPlayerRow>(RemovePlayer);
        ExitCommand = new Command(async () => await ExitAsync());
    }

    public GmEventLogViewModel EventLog { get; }
    public GmBestowViewModel Bestow { get; }
    public GmPlayersViewModel PlayerInterface { get; }
    public GmViewerViewModel Viewer { get; }

    // The GM's own scratch pad: one per campaign.
    public ScratchPadViewModel Pad { get; } = new();

    // The GM's maps: several per campaign, each with a picture, drawing and symbols.
    public GmMapViewModel Map { get; }

    public ObservableCollection<GmPlayerRow> Players { get; } = new();

    public ICommand BeginCombatCommand { get; }
    public ICommand EndCombatCommand { get; }
    public ICommand PassDayCommand { get; }
    public ICommand AddTestPlayerCommand { get; }
    public ICommand RemovePlayerCommand { get; }
    public ICommand ExitCommand { get; }

    // Raised when the calendar or clock changed from code (load, Pass a Day), so the page can move the slider.
    public event Action? ClockChanged;

    // ===== HEADER TEXT =====
    public string CampaignName => _campaign?.Name ?? string.Empty;
    public string DayHeader => _campaign == null
        ? string.Empty
        : _campaign.Calendar.DateFor(_campaign.Day).Header;

    public int Hour => _campaign?.Hour ?? 0;
    public int HourMax => Math.Max(0, (_campaign?.Calendar.HoursPerDay ?? 24) - 1);
    public string HourText => _campaign?.Calendar.HourLabel(Hour) ?? string.Empty;

    public bool HasNoPlayers => Players.Count == 0;

    // ===== SESSION OPEN / CLOSED =====
    // Closed by default. Opening and closing happen in the Players panel. Closing clones every connected
    // character into the campaign, then removes them from the session, so nobody stays connected to a closed session.
    public bool IsSessionOpen => _isOpen;

    public string SessionStateText => _isOpen
        ? "OPEN: players can join"
        : "CLOSED: nobody can join";

    private void RaiseSessionProperties()
    {
        OnPropertyChanged(nameof(IsSessionOpen));
        OnPropertyChanged(nameof(SessionStateText));
        PlayerInterface.RaiseSessionState();
    }

    private async Task ToggleSessionAsync()
    {
        if (_campaign == null) return;
        if (_isOpen) await CloseSessionAsync(askFirst: true);
        else await OpenSessionAsync();
    }

    private async Task OpenSessionAsync()
    {
        if (_campaign == null) return;

        _runId = Guid.NewGuid();   // a new session: dropped items can only be recovered within it

        try
        {
            var credentials = CampaignSessionCredentials.From(_campaign);   // null = no password
            await _net.StartAsync(_campaign.Id, _campaign.Name, credentials);   // the session starts listening, closed
            _net.SetOpen(true);
        }
        catch (Exception ex)
        {
            CrashLog.Write("GM: opening the session", ex);
            await Application.Current!.MainPage!.DisplayAlertAsync("Open Session", $"The session could not be started: {ex.Message}", "OK");
            return;
        }

        _isOpen = true;
        _campaign.IsOpen = true;
        RaiseSessionProperties();

        await PlayerInterface.ApplyNetworkAsync();   // lets players on the network in (if the GM has that switched on)
        await LogAsync(PlayerActionType.SessionOpened, "Opened the session to players");
    }

    // Returns false when the GM changed their mind.
    private async Task<bool> CloseSessionAsync(bool askFirst)
    {
        if (_campaign == null || !_isOpen) return true;

        if (askFirst)
        {
            string message = Players.Count == 0
                ? "Close the session? Nobody can join until you open it again."
                : $"Close the session? The {Players.Count} connected player(s) are saved, their characters are copied into the campaign, " +
                  "and they are removed from the session. They can rejoin when you open it again.";

            bool ok = await Application.Current!.MainPage!.DisplayAlertAsync("Close Session", message, "Close Session", "Cancel");
            if (!ok) return false;
        }

        // Players on the network save their character and send a fresh copy first (test players are read from disk below).
        GameNotices.Post(new GameNotice("💾 Saving characters", "Collecting a fresh copy from each player…", NoticeSource.Gm, 4));
        var copies = await _net.RequestSnapshotsAsync(TimeSpan.FromSeconds(15));
        var missing = copies.Where(c => !c.Received).ToList();
        if (missing.Count > 0 && askFirst)
        {
            string names = string.Join("\n", missing.Select(c => $"• {c.Player.CharacterName} ({c.Player.PlayerName}): {c.Message}"));
            bool closeAnyway = await Application.Current!.MainPage!.DisplayAlertAsync(
                "Some Copies Missing",
                $"A fresh copy of these characters couldn't be received:\n{names}\n\nTheir last stored copies are kept. Close the session anyway?",
                "Close Anyway", "Cancel");
            if (!closeAnyway) return false;
        }

        await SnapshotAsync();   // each character is read fresh and cloned into the campaign

        var removed = Players.ToList();
        foreach (var row in removed)
        {
            Players.Remove(row);
            await LogAsync(PlayerActionType.PlayerKicked, $"{row.Title} was saved and removed when the session closed");
        }
        OnPropertyChanged(nameof(HasNoPlayers));

        await _net.StopAsync();   // tells anyone still connected that the session closed
        _localJoinIds.Clear();
        PlayerInterface.SetRemotePlayers(Array.Empty<PlayerConnection>());
        PlayerInterface.RefreshNetwork();

        _isOpen = false;
        _campaign.IsOpen = false;
        await _store.SaveStateAsync(_campaign);
        RaiseSessionProperties();

        await LogAsync(PlayerActionType.SessionClosed, removed.Count == 0
            ? "Closed the session to players"
            : $"Closed the session: {removed.Count} player(s) saved and removed");
        return true;
    }

    // ===== COMBAT =====
    public bool CanBeginCombat => !_inCombat;
    public bool CanEndCombat => _inCombat;

    private async Task BeginCombatAsync()
    {
        if (_inCombat) return;
        _inCombat = true;
        OnPropertyChanged(nameof(CanBeginCombat));
        OnPropertyChanged(nameof(CanEndCombat));

        GameNotices.Post(new GameNotice("⚔️ Combat begins", "Your GM has started combat.", NoticeSource.Gm, 6));
        await LogAsync(PlayerActionType.CombatBegan, "Combat began");
    }

    private async Task EndCombatAsync()
    {
        if (!_inCombat) return;
        _inCombat = false;
        OnPropertyChanged(nameof(CanBeginCombat));
        OnPropertyChanged(nameof(CanEndCombat));

        GameNotices.Post(new GameNotice("🏳️ Combat ends",
            "Handle your end-of-combat routines: roll the usage die of every item you used.", NoticeSource.Gm, 8));
        await LogAsync(PlayerActionType.CombatEnded, "Combat ended");
    }

    // ===== TIME =====
    // The slider calls this while it moves; CommitHourAsync saves and logs once when the GM lets go.
    public void SetHour(int hour)
    {
        if (_campaign == null) return;
        _campaign.SetHour(hour);
        OnPropertyChanged(nameof(Hour));
        OnPropertyChanged(nameof(HourText));
    }

    public async Task CommitHourAsync()
    {
        if (_campaign == null) return;
        await _store.SaveStateAsync(_campaign);
        await LogAsync(PlayerActionType.TimeChanged, $"Time set to {HourText}");
    }

    private async Task PassDayAsync()
    {
        if (_campaign == null) return;

        var next = _campaign.Calendar.DateFor(_campaign.Day + 1);
        bool ok = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Pass a Day", $"Move on to {next.Header}? The time goes back to {_campaign.Calendar.HourLabel(0)}.", "Pass a Day", "Cancel");
        if (!ok) return;

        int from = _campaign.Day;
        _campaign.PassDay();
        await _store.SaveStateAsync(_campaign);

        RaiseDayProperties();
        ClockChanged?.Invoke();

        GameNotices.Post(new GameNotice("☀️ A new day", next.Header, NoticeSource.Gm, 6));
        await LogAsync(PlayerActionType.DayPassed, $"Day {from} passed: now {next.Header}");
    }

    private void RaiseDayProperties()
    {
        OnPropertyChanged(nameof(DayHeader));
        OnPropertyChanged(nameof(Hour));
        OnPropertyChanged(nameof(HourText));
    }

    // ===== PLAYERS =====
    // A test player is a saved character on this computer that joins the session through the real network core
    // (over the in-memory network), exactly as a remote player will. Real remote players arrive in a later step.
    private async Task AddTestPlayerAsync()
    {
        if (_campaign == null) return;
        var page = Application.Current!.MainPage!;

        // A closed session can't be joined.
        if (!_isOpen)
        {
            await page.DisplayAlertAsync("Add Test Player", "The session is closed, so nobody can join. Open it from the Players panel first.", "OK");
            return;
        }

        var saves = await _persistence.GetAllSavedCharactersAsync();
        var available = saves
            .Where(s => s.IsComplete && Players.All(p => p.Save.CharacterGuid != s.CharacterGuid))
            .ToList();

        if (available.Count == 0)
        {
            await page.DisplayAlertAsync("Add Test Player", "No other completed characters are saved on this computer.", "OK");
            return;
        }

        var labels = new Dictionary<string, CharacterSaveData>();
        foreach (var save in available)
        {
            string label = $"{save.CharacterName} ({save.PlayerName})";
            string unique = label;
            int n = 2;
            while (labels.ContainsKey(unique)) unique = $"{label} #{n++}";
            labels[unique] = save;
        }

        string? choice = await page.DisplayActionSheetAsync("Add a test player", "Cancel", null, labels.Keys.ToArray());
        if (choice == null || !labels.TryGetValue(choice, out var picked)) return;

        var fresh = await _persistence.LoadCharacterAsync(picked.FileName) ?? picked;

        // A character that was in this campaign before must match what was saved when the session last closed.
        var report = await _store.ValidateCharacterAsync(_campaign, fresh);
        if (report.HadSnapshot && !report.Matches)
        {
            string changed = report.ChangedSections.Count == 0
                ? "its data differs from the saved copy"
                : string.Join(", ", report.ChangedSections);

            await LogAsync(PlayerActionType.TamperDetected,
                $"{fresh.CharacterName} ({fresh.PlayerName}) changed outside the session: {changed}");

            bool allow = await page.DisplayAlertAsync(
                "Character Changed",
                $"{fresh.CharacterName} was changed since this session last closed.\n\nChanged: {changed}",
                "Allow Anyway", "Refuse");
            if (!allow) return;
        }

        // Join through the network core. The session decides (open, not full, not already in).
        var hello = new HelloMessage
        {
            AppVersion = AppInfo.VersionString,
            PlayerId = Guid.NewGuid(),
            PlayerName = fresh.PlayerName,
            CharacterGuid = fresh.CharacterGuid,
            CharacterName = fresh.CharacterName
        };

        _localJoinIds.Add(hello.PlayerId);   // before joining, so the join isn't mistaken for a network player
        var result = await _net.JoinLocalPlayerAsync(hello);
        if (!result.Accepted)
        {
            _localJoinIds.Remove(hello.PlayerId);
            await page.DisplayAlertAsync("Add Test Player", $"{fresh.CharacterName} could not join: {result.Message}", "OK");
            return;
        }

        Players.Add(await CreateRowAsync(fresh, hello.PlayerId));
        OnPropertyChanged(nameof(HasNoPlayers));

        await ImportHistoryAsync(fresh);
    }

    // Test connections only: a real connection reports its events live, so this goes away with networking.
    // Copies the character's latest events from the shared log, so the GM log has something to show.
    private async Task ImportHistoryAsync(CharacterSaveData character)
    {
        var all = await _log.GetAllEntriesAsync();
        var history = all
            .Where(e => e.CharacterGuid == character.CharacterGuid)
            .OrderByDescending(e => e.Timestamp)
            .Take(HistoryImportCount)
            .ToList();

        if (history.Count > 0) await _gmLog.ImportAsync(history);
    }

    // Takes the player off the dashboard and out of the session.
    private void RemovePlayer(GmPlayerRow? row)
    {
        if (row == null) return;
        Players.Remove(row);
        OnPropertyChanged(nameof(HasNoPlayers));
        if (row.PlayerId != Guid.Empty) _ = _net.KickAsync(row.PlayerId);
    }

    // ===== NETWORK EVENTS =====
    // Players who lost their connection and haven't come back or left, so one drop is logged once.
    private readonly HashSet<Guid> _netDown = new();

    private static string NetTitle(PlayerConnection player) => $"{player.CharacterName} ({player.PlayerName})";

    // Joins, leaves and drops go in the GM's event log only. Players are not told.
    private async void LogNetEvent(PlayerActionType type, string summary)
    {
        try { await LogAsync(type, summary); }
        catch (Exception ex) { CrashLog.Write("GM: logging a network event", ex); }
    }
    // The hub reports these on pool threads, so each one hops to the UI thread before touching Players.
    private void AttachNetwork()
    {
        _net.OnPlayerJoined = OnNetPlayerJoined;
        _net.OnPlayerStateChanged = OnNetPlayerStateChanged;
        _net.OnPlayerLeft = OnNetPlayerLeft;
        _net.OnCharacterSnapshot = OnCharacterSnapshotAsync;
        _net.OnCharacterSummary = (_, _) => MainThread.BeginInvokeOnMainThread(() => RefreshRemotePlayers());
    }

    private void DetachNetwork()
    {
        _net.OnPlayerJoined = null;
        _net.OnPlayerStateChanged = null;
        _net.OnPlayerLeft = null;
        _net.OnCharacterSnapshot = null;
        _net.OnCharacterSummary = null;
    }

    // A character joining over the network is compared with the copy stored when it last left or the session last
    // closed. The join is already accepted (a dialog can't hold a join open), so Remove is a kick.
    private async Task CheckReturningPlayerAsync(PlayerConnection player)
    {
        try
        {
            var campaign = _campaign;
            if (campaign == null || string.IsNullOrEmpty(player.DataHash)) return;

            var record = campaign.Characters.FirstOrDefault(r => r.CharacterGuid == player.CharacterGuid);
            if (record == null || record.SnapshotHash == player.DataHash) return;   // new to this campaign, or unchanged

            var snapshot = await _store.GetSnapshotAsync(campaign, player.CharacterGuid);
            var changed = snapshot == null
                ? new List<string>()
                : CharacterSnapshots.ChangedSections(snapshot.Data, player.SectionHashes);

            string what = changed.Count == 0 ? "its data differs from the stored copy" : string.Join(", ", changed);
            string who = $"{player.CharacterName} ({player.PlayerName})";

            await LogAsync(PlayerActionType.TamperDetected, $"{who} changed outside the session: {what}");

            bool keep = await Application.Current!.MainPage!.DisplayAlertAsync(
                "Character Changed",
                $"{who} was changed since the last copy you stored.\n\nChanged: {what}",
                "Keep Them In", "Remove");
            if (!keep) await _net.KickAsync(player.PlayerId);
        }
        catch (Exception ex)
        {
            CrashLog.Write("GM: checking a returning player", ex);
        }
    }

    // A network player sent a copy of their character (they are leaving, or the session is closing). This runs on a
    // pool thread, and the campaign belongs to the screen, so the copy is stored on the UI thread. A damaged copy
    // throws: the hub answers "not stored" and the player is told.
    private async Task<bool> OnCharacterSnapshotAsync(PlayerConnection player, CharacterSnapshotMessage message)
    {
        CharacterSaveData? save = CharacterSnapshots.Unpack(message);
        if (save == null || save.CharacterGuid != player.CharacterGuid) return false;

        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (_campaign == null) return false;
            await _store.SnapshotCharactersAsync(_campaign, new[] { save });
            return true;
        });
    }

    // The players who joined over the network are listed from the hub's roster: everyone who isn't a test player.
    private void RefreshRemotePlayers(Guid? except = null)
    {
        var remote = _net.HostPlayers
            .Where(p => !_localJoinIds.Contains(p.PlayerId) && p.PlayerId != except)
            .ToList();
        PlayerInterface.SetRemotePlayers(remote);
    }

    // A first join by a test player is added by whoever started it (Add Test Player); a returning player needs handling here.
    private void OnNetPlayerJoined(PlayerConnection player, bool resumed)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            RefreshRemotePlayers();

            _netDown.Remove(player.PlayerId);
            LogNetEvent(resumed ? PlayerActionType.PlayerReconnected : PlayerActionType.PlayerJoined,
                resumed ? $"{NetTitle(player)} reconnected" : $"{NetTitle(player)} joined the session");

            if (!resumed)
            {
                if (!_localJoinIds.Contains(player.PlayerId))
                {
                    GameNotices.Post(new GameNotice("🔌 Joined", $"{player.CharacterName} ({player.PlayerName}) joined over the network.", NoticeSource.Player, 5));
                    _ = CheckReturningPlayerAsync(player);
                }
                return;
            }

            var row = Players.FirstOrDefault(r => r.PlayerId == player.PlayerId);
            if (row == null) return;

            row.IsConnected = true;
            GameNotices.Post(new GameNotice("🔌 Reconnected", $"{row.Title} is back in the session.", NoticeSource.Player, 5));
        });
    }

    private void OnNetPlayerStateChanged(PlayerConnection player)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            RefreshRemotePlayers();

            if (!player.IsConnected && _netDown.Add(player.PlayerId))
                LogNetEvent(PlayerActionType.PlayerDisconnected, $"{NetTitle(player)} lost their connection");

            var row = Players.FirstOrDefault(r => r.PlayerId == player.PlayerId);
            if (row == null || row.IsConnected == player.IsConnected) return;

            row.IsConnected = player.IsConnected;
            if (!player.IsConnected)
                GameNotices.Post(new GameNotice("⚠️ Connection lost", $"{row.Title} lost their connection. They keep their place and can rejoin.", NoticeSource.Player, 6));
        });
    }

    private void OnNetPlayerLeft(PlayerConnection player, PlayerLeaveReason reason)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _localJoinIds.Remove(player.PlayerId);
            RefreshRemotePlayers(except: player.PlayerId);

            _netDown.Remove(player.PlayerId);
            if (reason == PlayerLeaveReason.Left)
                LogNetEvent(PlayerActionType.PlayerLeft, $"{NetTitle(player)} left the session");

            var row = Players.FirstOrDefault(r => r.PlayerId == player.PlayerId);
            if (row == null) return;

            Players.Remove(row);
            OnPropertyChanged(nameof(HasNoPlayers));

            if (reason == PlayerLeaveReason.Left)
                GameNotices.Post(new GameNotice("🚪 Left the session", $"{row.Title} left the session.", NoticeSource.Player, 5));
        });
    }

    // Reads each connected character fresh from disk and clones it into the campaign.
    private async Task SnapshotAsync()
    {
        if (_campaign == null) return;

        var current = new List<CharacterSaveData>();
        foreach (var row in Players)
        {
            var fresh = await _persistence.LoadCharacterAsync(row.Save.FileName);
            if (fresh != null) current.Add(fresh);
        }

        await _store.SnapshotCharactersAsync(_campaign, current);
    }

    // After the GM changes saved characters, the player strip shows their latest HP and Vigor.
    private async Task RefreshPlayersAsync()
    {
        foreach (var row in Players.ToList())
        {
            var fresh = await _persistence.LoadCharacterAsync(row.Save.FileName);
            if (fresh == null) continue;

            int at = Players.IndexOf(row);   // the GM may have kicked them while this was loading
            if (at >= 0) Players[at] = await CreateRowAsync(fresh, row.PlayerId, row.IsConnected);
        }
    }

    private async Task<GmPlayerRow> CreateRowAsync(CharacterSaveData save, Guid playerId = default, bool isConnected = true)
        => new(save, await _characters.GetSummaryAsync(save), playerId, isConnected);

    // ===== THE GM'S OWN WORK (scratch pad and maps) =====
    public Task OpenPadAsync()
    {
        if (_campaign == null) return Task.CompletedTask;
        var campaignId = _campaign.Id;
        return Pad.OpenAsync(campaignId, () => new ScratchPadStore(_store.PadFolder(campaignId)));
    }

    // Saves the GM's own work: the scratch pad and the open map.
    public Task SavePadAsync() => Task.WhenAll(Pad.SaveAsync(), Map.SaveAsync());

    // ===== LOADING / EXITING =====
    public async Task LoadAsync(string? campaignIdText)
    {
        if (!Guid.TryParse(campaignIdText, out var campaignId)) return;

        var campaign = await _store.LoadCampaignAsync(campaignId);
        if (campaign == null)
        {
            await Application.Current!.MainPage!.DisplayAlertAsync("Campaign Not Found", "That campaign no longer exists.", "OK");
            await Shell.Current.GoToAsync("///CampaignListPage");
            return;
        }

        await _net.StopAsync();   // a session left running from an earlier visit
        _localJoinIds.Clear();
        PlayerInterface.SetRemotePlayers(Array.Empty<PlayerConnection>());
        PlayerInterface.RefreshNetwork();
        AttachNetwork();

        _campaign = campaign;
        _campaign.IsOpen = false;   // a campaign is always closed when it is loaded
        _isOpen = false;
        _inCombat = false;
        _runId = Guid.NewGuid();

        Players.Clear();

        campaign.LastOpenedUtc = DateTime.UtcNow;
        await _store.SaveStateAsync(campaign);

        await _gmLog.AttachAsync(campaignId);   // the campaign's event log starts listening

        PlayerInterface.RefreshSession();       // shows this campaign's session, policy and password state
        await Map.OpenAsync(campaignId);        // the campaign's maps and symbol library

        OnPropertyChanged(string.Empty);
        ClockChanged?.Invoke();
    }

    private async Task ExitAsync()
    {
        if (_campaign == null) return;

        string message = _isOpen && Players.Count > 0
            ? $"The campaign is saved. The {Players.Count} connected player(s) are saved and removed, and the session closes."
            : "The campaign is saved and the session closes.";

        bool ok = await Application.Current!.MainPage!.DisplayAlertAsync("Save & Exit", message, "Save & Exit", "Cancel");
        if (!ok) return;

        if (_isOpen) await CloseSessionAsync(askFirst: false);   // already confirmed above

        await SavePadAsync();   // anything written on the pad or drawn on a map is kept
        await _store.SaveStateAsync(_campaign);

        await _gmLog.DetachAsync();   // writes the log to disk and stops listening
        DetachNetwork();

        await Shell.Current.GoToAsync("///CampaignListPage");
    }

    // ===== LOG =====
    // GM actions use the GM source, so the event log can show or hide them.
    private Task LogAsync(PlayerActionType type, string summary)
        => _log.LogEventAsync(new PlayerActionLogEntry
        {
            ActionType = type,
            Source = ActionSource.GM,
            PlayerName = "GM",
            PlayerId = Guid.Empty,
            CharacterName = _campaign?.Name ?? string.Empty,
            CharacterGuid = Guid.Empty,
            SessionId = _runId,
            Summary = summary
        });

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}