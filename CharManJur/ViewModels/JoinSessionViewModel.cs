using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

// One session the player's app heard announced on the network.
public sealed class FoundSessionRow
{
    public FoundSessionRow(FoundSession found)
    {
        var announcement = found.Announcement;
        Address = found.Address;
        Title = announcement.SessionName;
        PasswordRequired = announcement.PasswordRequired;
        Details = $"{found.Address} · {announcement.PlayerCount} player(s) · " +
                  (announcement.IsOpen ? "open" : "closed") +
                  (announcement.PasswordRequired ? " · 🔒 password" : string.Empty);
    }

    public string Address { get; }
    public string Title { get; }
    public string Details { get; }
    public bool PasswordRequired { get; }
}

public class JoinSessionViewModel : INotifyPropertyChanged
{
    private readonly ICharacterPersistenceService _persistence;
    private readonly IPlayerSession _session;

    private List<CharacterSaveData> _characters = new();
    private SessionFinder? _finder;
    private int _selectedIndex = -1;
    private string _address = string.Empty;
    private string _password = string.Empty;
    private string _status = string.Empty;
    private string _error = string.Empty;
    private bool _busy;

    public JoinSessionViewModel(ICharacterPersistenceService persistence, IPlayerSession session)
    {
        _persistence = persistence;
        _session = session;

        JoinCommand = new Command(async () => await JoinAsync());
        LeaveCommand = new Command(async () => await LeaveAsync());
        OpenCharacterCommand = new Command(async () => await Shell.Current.GoToAsync("///CharacterHomePage"));
    }

    public ICommand JoinCommand { get; }
    public ICommand LeaveCommand { get; }
    public ICommand OpenCharacterCommand { get; }

    public ObservableCollection<FoundSessionRow> Sessions { get; } = new();
    public bool HasSessions => Sessions.Count > 0;
    public bool HasNoSessions => Sessions.Count == 0;

    // ===== CHARACTER =====
    public List<string> CharacterLabels => _characters.Select(c => $"{c.CharacterName} ({c.PlayerName})").ToList();
    public bool HasNoCharacters => _characters.Count == 0;

    public int SelectedCharacterIndex
    {
        get => _selectedIndex;
        set
        {
            _selectedIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanJoin));
        }
    }

    // ===== CONNECTING =====
    public string Address
    {
        get => _address;
        set
        {
            _address = value ?? string.Empty;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanJoin));
        }
    }

    public string Password
    {
        get => _password;
        set
        {
            _password = value ?? string.Empty;
            OnPropertyChanged();
        }
    }

    public string StatusText
    {
        get => _status;
        private set { _status = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasStatus)); }
    }

    public string ErrorText
    {
        get => _error;
        private set { _error = value; OnPropertyChanged(); OnPropertyChanged(nameof(HasError)); }
    }

    public bool HasStatus => !string.IsNullOrEmpty(_status);
    public bool HasError => !string.IsNullOrEmpty(_error);

    public bool CanJoin => !_busy && !InSession
        && _selectedIndex >= 0 && _selectedIndex < _characters.Count
        && _address.Trim().Length > 0;

    // ===== WHILE CONNECTED =====
    public bool InSession => _session.IsInSession;
    public bool ShowJoinForm => !InSession;
    public string ConnectedBanner => $"✅ Connected to {_session.SessionName} as {_session.CharacterName}";
    public string ConnectionNote => _session.State == ClientState.Reconnecting
        ? "⚠️ Connection lost. Trying to reconnect. You keep your place."
        : string.Empty;
    public bool HasConnectionNote => ConnectionNote.Length > 0;

    // ===== LIFETIME =====
    public async Task StartAsync()
    {
        _session.Changed -= OnSessionChanged;
        _session.Changed += OnSessionChanged;
        RaiseSessionProperties();

        await LoadCharactersAsync();
        StartFinder();
    }

    public void Stop()
    {
        _session.Changed -= OnSessionChanged;
        StopFinder();
    }

    private async Task LoadCharactersAsync()
    {
        Guid? keep = _selectedIndex >= 0 && _selectedIndex < _characters.Count
            ? _characters[_selectedIndex].CharacterGuid
            : null;

        var all = await _persistence.GetAllSavedCharactersAsync();
        _characters = all.Where(c => c.IsComplete).OrderBy(c => c.CharacterName).ToList();

        OnPropertyChanged(nameof(CharacterLabels));
        OnPropertyChanged(nameof(HasNoCharacters));

        int index = keep == null ? -1 : _characters.FindIndex(c => c.CharacterGuid == keep);
        if (index < 0 && _characters.Count > 0) index = 0;
        SelectedCharacterIndex = index;
    }

    // ===== FINDING SESSIONS =====
    private void StartFinder()
    {
        if (_finder != null) return;

        try
        {
            var finder = new SessionFinder();
            finder.Changed += OnFinderChanged;
            finder.Start();
            _finder = finder;
        }
        catch (Exception ex)
        {
            CrashLog.Write("Player: looking for sessions", ex);
            StatusText = "Searching for sessions isn't available here. Use the invite code instead.";
        }
    }

    private void StopFinder()
    {
        var finder = _finder;
        _finder = null;
        if (finder == null) return;

        finder.Changed -= OnFinderChanged;
        _ = DisposeFinderAsync(finder);
    }

    private static async Task DisposeFinderAsync(SessionFinder finder)
    {
        try { await finder.DisposeAsync(); }
        catch (Exception ex) { CrashLog.Write("Player: stopping the session search", ex); }
    }

    // Announcements arrive on a pool thread; the list is only touched on the UI thread.
    private void OnFinderChanged()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var finder = _finder;
            if (finder == null) return;

            var rows = finder.Sessions.Select(s => new FoundSessionRow(s)).ToList();

            for (int i = Sessions.Count - 1; i >= 0; i--)
                if (rows.All(r => r.Address != Sessions[i].Address)) Sessions.RemoveAt(i);

            foreach (var row in rows)
            {
                int at = -1;
                for (int i = 0; i < Sessions.Count; i++)
                    if (Sessions[i].Address == row.Address) { at = i; break; }

                if (at < 0) Sessions.Add(row);
                else if (Sessions[at].Details != row.Details || Sessions[at].Title != row.Title) Sessions[at] = row;
            }

            OnPropertyChanged(nameof(HasSessions));
            OnPropertyChanged(nameof(HasNoSessions));
        });
    }

    // The Use button on a found session fills in the address.
    public void UseSession(FoundSessionRow row)
    {
        Address = row.Address;
        ErrorText = string.Empty;
    }

    // ===== JOIN / LEAVE =====
    private async Task JoinAsync()
    {
        if (!CanJoin) return;

        ErrorText = string.Empty;
        string? address = ResolveAddress(_address, out string problem);
        if (address == null)
        {
            ErrorText = problem;
            return;
        }

        SetBusy(true);
        StatusText = "Connecting…";
        try
        {
            var result = await _session.JoinAsync(
                address, _characters[_selectedIndex], string.IsNullOrEmpty(_password) ? null : _password);

            if (!result.Accepted)
            {
                ErrorText = result.Message;
                return;
            }

            Password = string.Empty;
            await Shell.Current.GoToAsync("///CharacterHomePage");
        }
        catch (Exception ex)
        {
            CrashLog.Write("Player: join screen", ex);
            ErrorText = $"Could not join: {ex.Message}";
        }
        finally
        {
            StatusText = string.Empty;
            SetBusy(false);
            RaiseSessionProperties();
        }
    }

    private async Task LeaveAsync()
    {
        bool leave = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Leave Session", "Leave the GM's session? You can rejoin while it is open.", "Leave", "Stay");
        if (!leave) return;

        await _session.LeaveAsync();
        RaiseSessionProperties();
    }

    // An invite code decodes to an address; anything with a dot or colon (or just a port) is used as typed.
    private static string? ResolveAddress(string text, out string problem)
    {
        problem = string.Empty;
        text = text.Trim();

        var decoded = InviteCode.TryDecode(text);
        if (decoded != null) return decoded.HostAddress;

        bool looksLikeAddress = text.Contains('.') || text.Contains(':') || text.All(char.IsDigit);
        if (!looksLikeAddress && text.Length >= 6)
        {
            problem = "That invite code isn't valid. Check it for typos, or type the address instead.";
            return null;
        }

        return text;
    }

    private void SetBusy(bool busy)
    {
        _busy = busy;
        OnPropertyChanged(nameof(CanJoin));
    }

    private void OnSessionChanged() => MainThread.BeginInvokeOnMainThread(RaiseSessionProperties);

    private void RaiseSessionProperties()
    {
        OnPropertyChanged(nameof(InSession));
        OnPropertyChanged(nameof(ShowJoinForm));
        OnPropertyChanged(nameof(ConnectedBanner));
        OnPropertyChanged(nameof(ConnectionNote));
        OnPropertyChanged(nameof(HasConnectionNote));
        OnPropertyChanged(nameof(CanJoin));
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}