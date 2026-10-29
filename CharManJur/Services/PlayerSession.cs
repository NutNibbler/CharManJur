using CharManJur.Models;
using CharManJur.ViewModels.Godrick_LiveGame;
using System.Text.Json;

namespace CharManJur.Services;

public sealed class PlayerSession : IPlayerSession
{
    private readonly ICharAttribDataService _charData;
    private readonly IGlobalMenuDataService _menu;
    private readonly ICharacterPersistenceService _persistence;

    private SessionClient? _client;
    private bool _joined;   // the GM accepted us and the character is loaded
    private bool _busy;
    private string _characterName = string.Empty;
    private CancellationTokenSource? _summaryLoop;   // sends the live stat chips to the GM while joined
    private string? _lastSummaryKey;                 // what the GM's card shows now; touched only on the UI thread
    private Guid _characterGuid;
    // GM actions already applied this session, so a resend after a flaky link is confirmed but not applied twice.
    private readonly HashSet<Guid> _appliedActions = new();
    private readonly Queue<Guid> _appliedOrder = new();
    private static readonly JsonSerializerOptions ActionJson = new() { PropertyNameCaseInsensitive = true };

    private readonly IItemDataService _items;
    private readonly IPlayerActionLogService _actionLog;

    public PlayerSession(
        ICharAttribDataService charData,
        IGlobalMenuDataService menu,
        ICharacterPersistenceService persistence,
        IItemDataService items,
        IPlayerActionLogService actionLog)
    {
        _charData = charData;
        _menu = menu;
        _persistence = persistence;
        _items = items;
        _actionLog = actionLog;
    }

    public bool IsInSession => _joined;
    public ClientState State => _client?.State ?? ClientState.Idle;
    public string SessionName => _client?.Roster?.SessionName is { Length: > 0 } name ? name : "the GM's session";
    public string CharacterName => _characterName;

    public event Action? Changed;

    public async Task<JoinResult> JoinAsync(string address, CharacterSaveData character, string? password)
    {
        if (_busy || _joined) return new JoinResult(false, null, "You are already in a session. Leave it first.");
        _busy = true;

        try
        {
            var fresh = await _persistence.LoadCharacterAsync(character.FileName) ?? character;

            var hello = new HelloMessage
            {
                AppVersion = AppInfo.VersionString,
                PlayerId = _charData.PlayerId,   // this install's id, so a returning player is recognised
                PlayerName = fresh.PlayerName,
                CharacterGuid = fresh.CharacterGuid,
                CharacterName = fresh.CharacterName,
                DataHash = CharacterSnapshots.HashData(fresh.Data),                  // what the GM compares with the copy it holds
                SectionHashes = CharacterSnapshots.SectionHashes(fresh.Data)
            };

            var client = new SessionClient(new TcpTransport());
            client.StateChanged += OnClientStateChanged;
            client.MessageReceived += OnClientMessage;
            _client = client;

            var result = await client.JoinAsync(address, hello, password);
            if (!result.Accepted)
            {
                await DropClientAsync(wasJoined: false);
                return result;
            }

            // Same steps as Load Selected on the Load Character screen.
            await _charData.PopulateFromSaveDataAsync(fresh);
            _menu.CharBuilderLoadRequest();
            _menu.SetCharacterCreationMode(true);

            _characterName = fresh.CharacterName;
            _joined = true;
            _characterGuid = fresh.CharacterGuid;
            StartSummaryLoop(client);
            OnMain(() =>
            {
                SessionMode.SetGmHosted(true);
                Changed?.Invoke();
            });
            return result;
        }
        catch (Exception ex)
        {
            CrashLog.Write("Player: joining a session", ex);
            await DropClientAsync(wasJoined: _joined);
            return new JoinResult(false, null, $"Could not join: {ex.Message}");
        }
        finally
        {
            _busy = false;
        }
    }

    // ---------------------------------------------------------------- copies for the GM

    // The caller has just saved the character: send that saved file to the GM and wait for "stored".
    public async Task<bool> SendFinalCopyAsync()
    {
        var client = _client;
        if (client == null || !_joined) return true;   // no session, nothing to send

        try
        {
            var save = string.IsNullOrEmpty(_charData.FileName) ? null : await _persistence.LoadCharacterAsync(_charData.FileName);
            if (save == null) return false;

            var reply = await client.RequestAsync(
                NetMessageTypes.CharacterSnapshot, CharacterSnapshots.Pack(save, "leaving"), TimeSpan.FromSeconds(15));
            return EnvelopeCodec.ReadPayload<AckMessage>(reply)?.Ok == true;
        }
        catch (Exception ex)
        {
            CrashLog.Write("Player: sending the final copy to the GM", ex);
            return false;
        }
    }

    // The GM is closing the session and asks for a fresh copy: save here first (so both ends match), then send it.
    private void OnClientMessage(Envelope envelope)
    {
        var client = _client;
        if (client == null) return;

        if (envelope.Type == NetMessageTypes.SnapshotRequest) _ = AnswerSnapshotRequestAsync(client, envelope);
        else if (envelope.Type == NetMessageTypes.GmAction) _ = ApplyGmActionAsync(client, envelope);
    }

    private async Task AnswerSnapshotRequestAsync(SessionClient client, Envelope request)
    {
        try
        {
            CharacterSaveData? save = _joined ? await SaveAndReadBackAsync() : null;
            if (save == null)
            {
                await client.ReplyAsync(request, NetMessageTypes.Ack,
                    new AckMessage { Ok = false, Message = "The player's character could not be saved." });
                return;
            }

            await client.ReplyAsync(request, NetMessageTypes.CharacterSnapshot, CharacterSnapshots.Pack(save, "session closing"));
        }
        catch (Exception ex)
        {
            CrashLog.Write("Player: answering the GM's request for a copy", ex);
            try
            {
                await client.ReplyAsync(request, NetMessageTypes.Ack, new AckMessage { Ok = false, Message = "The player's app hit an error." });
            }
            catch (Exception) { /* the link is gone: the GM will see the timeout */ }
        }
    }

    // Saves the live character with the app's normal save, then reads the file back so what the GM gets is exactly what is on disk.
    private async Task<CharacterSaveData?> SaveAndReadBackAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(() => _charData.SaveNowAsync());
        return string.IsNullOrEmpty(_charData.FileName) ? null : await _persistence.LoadCharacterAsync(_charData.FileName);
    }

    // ---------------------------------------------------------------- leaving

    public async Task LeaveAsync()
    {
        var client = _client;
        if (client == null) return;

        try { await client.LeaveAsync(); }
        catch (Exception ex) { CrashLog.Write("Player: leaving the session", ex); }

        await DropClientAsync(wasJoined: _joined);
        OnMain(() => Changed?.Invoke());
    }

    // The client reports on pool threads: everything that follows happens on the UI thread.
    private void OnClientStateChanged(ClientState state)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            _lastSummaryKey = null;   // after a lost connection the GM's card may be stale: send the chips again
            try
            {
                if (!_joined) return;   // the join itself reports its own result

                if (state is ClientState.Kicked or ClientState.SessionClosed or ClientState.Rejected or ClientState.Disconnected)
                    await EndedAsync(state);
                else
                    Changed?.Invoke();   // connected again / connection lost, retrying
            }
            catch (Exception ex)
            {
                CrashLog.Write("Player: session state change", ex);
            }
        });
    }

    // The GM removed us, closed the session, or the link is gone for good.
    private async Task EndedAsync(ClientState state)
    {
        string reason = _client?.LastMessage ?? string.Empty;
        await DropClientAsync(wasJoined: true);

        var (title, fallback) = state switch
        {
            ClientState.Kicked => ("🥾 Removed from the session", "The GM removed you from the session."),
            ClientState.SessionClosed => ("🔒 Session closed", "The GM closed the session."),
            _ => ("🔌 Disconnected", "The connection to the GM ended.")
        };

        Changed?.Invoke();

        await Shell.Current.GoToAsync("///MainPage");

        // A toast would disappear with the character screen, so the player is told with a dialog on the menu.
        await Application.Current!.MainPage!.DisplayAlertAsync(
            title, string.IsNullOrWhiteSpace(reason) ? fallback : reason, "OK");
    }

    private async Task DropClientAsync(bool wasJoined)
    {
        StopSummaryLoop();
        _appliedActions.Clear();
        _appliedOrder.Clear();
        var client = _client;
        _client = null;
        _joined = false;

        if (client != null)
        {
            client.StateChanged -= OnClientStateChanged;
            client.MessageReceived -= OnClientMessage;
            try { await client.DisposeAsync(); }
            catch (Exception ex) { CrashLog.Write("Player: closing the connection", ex); }
        }

        if (wasJoined)
        {
            OnMain(() => SessionMode.SetGmHosted(false));
            try { await _charData.EndSessionAsync(); }
            catch (Exception ex) { CrashLog.Write("Player: ending the session", ex); }
        }
    }

    // ---------------------------------------------------------------- GM actions

    // The GM sent damage or an effect: queue it on the live character (like the GM's file edit does for test
    // players) and confirm. The Apply Damage / Apply Effect buttons then flash until the player responds.
    private async Task ApplyGmActionAsync(SessionClient client, Envelope request)
    {
        AckMessage ack;
        try
        {
            var action = EnvelopeCodec.ReadPayload<GmActionMessage>(request);
            ack = action == null || !_joined
                ? new AckMessage { Ok = false, Message = "The player's app is not in the session." }
                : action.Kind == "bestow"
                    ? await MainThread.InvokeOnMainThreadAsync(async () => await ApplyBestowAsync(action))
                    : await MainThread.InvokeOnMainThreadAsync(() => ApplyGmAction(action));
        }
        catch (Exception ex)
        {
            CrashLog.Write("Player: applying a GM action", ex);
            ack = new AckMessage { Ok = false, Message = "The player's app could not apply it." };
        }

        try { await client.ReplyAsync(request, NetMessageTypes.Ack, ack); }
        catch (Exception ex) { CrashLog.Write("Player: confirming a GM action", ex); }
    }

    // UI thread. The GM bestowed items or a familiar: check that everything can be done, then do it all.
    private async Task<AckMessage> ApplyBestowAsync(GmActionMessage action)
    {
        if (_appliedActions.Contains(action.ActionId))
            return new AckMessage { Ok = true, Message = "Already received." };

        var ops = JsonSerializer.Deserialize<List<RemoteBestowOp>>(action.Json, ActionJson);
        var plan = await PlayerBestow.PrepareAsync(ops, _charData, _items, _actionLog);
        if (plan.Apply == null)
            return new AckMessage { Ok = false, Message = plan.Error };

        string result;
        var home = IPlatformApplication.Current?.Services.GetService<CharacterHomeViewModel>();
        if (home != null)
        {
            result = string.Empty;
            await home.ApplyGmBestowAsync(() => result = plan.Apply());
        }
        else
        {
            result = plan.Apply();
        }

        _appliedActions.Add(action.ActionId);
        _appliedOrder.Enqueue(action.ActionId);
        while (_appliedOrder.Count > 200) _appliedActions.Remove(_appliedOrder.Dequeue());

        return new AckMessage { Ok = true, Message = result };
    }

    // UI thread: the character service belongs to it.
    private AckMessage ApplyGmAction(GmActionMessage action)
    {
        static AckMessage Refuse(string why) => new() { Ok = false, Message = why };

        if (_appliedActions.Contains(action.ActionId))
            return new AckMessage { Ok = true, Message = "Already received." };

        switch (action.Kind)
        {
            case "damage":
                var damage = JsonSerializer.Deserialize<IncomingDamage>(action.Json, ActionJson);
                if (damage == null) return Refuse("The damage could not be read.");
                damage.Amount = Math.Clamp(damage.Amount, 0, 9999);
                _charData.QueueIncomingDamage(damage);
                break;

            case "effect":
                var effect = JsonSerializer.Deserialize<IncomingEffect>(action.Json, ActionJson);
                if (effect?.Effect == null) return Refuse("The effect could not be read.");
                _charData.QueueIncomingEffect(effect);
                break;

            default:
                return Refuse($"This version of the app doesn't know the action '{action.Kind}'.");
        }

        _appliedActions.Add(action.ActionId);
        _appliedOrder.Enqueue(action.ActionId);
        while (_appliedOrder.Count > 200) _appliedActions.Remove(_appliedOrder.Dequeue());

        IPlatformApplication.Current?.Services.GetService<CharacterHomeViewModel>()?.RefreshIncoming();
        return new AckMessage { Ok = true, Message = "Queued." };
    }

    // ---------------------------------------------------------------- live stat chips for the GM's card

    private void StartSummaryLoop(SessionClient client)
    {
        StopSummaryLoop();
        var cts = new CancellationTokenSource();
        _summaryLoop = cts;
        _lastSummaryKey = null;
        _ = SummaryLoopAsync(client, cts.Token);
    }

    private void StopSummaryLoop()
    {
        var cts = _summaryLoop;
        _summaryLoop = null;
        try { cts?.Cancel(); }
        catch (Exception ex) { CrashLog.Write("Player: stopping the stat updates", ex); }
    }

    private async Task SummaryLoopAsync(SessionClient client, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                // Built and compared on the UI thread, because the character service belongs to it.
                var summary = await MainThread.InvokeOnMainThreadAsync<CharacterSummaryMessage?>(() =>
                {
                    var built = CharacterSummaryBuilder.Build(_charData, _characterGuid);
                    string key = CharacterSummaryBuilder.Key(built);
                    if (key == _lastSummaryKey) return null;

                    _lastSummaryKey = key;
                    return built;
                });

                if (summary != null && !ct.IsCancellationRequested)
                {
                    try { await client.SendAsync(NetMessageTypes.CharacterSummary, summary); }
                    catch (Exception)
                    {
                        _lastSummaryKey = null;   // not delivered (the link is down): try again on the next round
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(2), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                CrashLog.Write("Player: sending stat updates to the GM", ex);
                try { await Task.Delay(TimeSpan.FromSeconds(5), ct); }
                catch (OperationCanceledException) { break; }
            }
        }
    }

    private static void OnMain(Action action)
    {
        if (MainThread.IsMainThread) action();
        else MainThread.BeginInvokeOnMainThread(action);
    }
}