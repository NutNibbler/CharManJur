using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

// One line of the tray: a day heading or an event. A flat list, rebuilt from scratch each time,
// because editing a grouped list that is on screen crashes on Windows.
public class EventTrayRow
{
    public bool IsHeader { get; init; }
    public bool IsEntry => !IsHeader;
    public string HeaderText { get; init; } = string.Empty;

    public string Icon { get; init; } = "•";
    public string Label { get; init; } = string.Empty;
    public string TimeText { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public bool IsGm { get; init; }
    public bool IsRuleSkating { get; init; }
    public bool ShowAdvanced { get; init; }
    public string AdvancedText { get; init; } = string.Empty;
}

// The player's event history: everything that happened to this character, newest first, kept live.
// The filter decides whose events appear, so the future GM dashboard can reuse this with a different one.
public class EventTrayViewModel : INotifyPropertyChanged
{
    private const int PageSize = 150;

    private readonly IPlayerActionLogService _log;
    private readonly Func<PlayerActionLogEntry, bool> _filter;

    private List<PlayerActionLogEntry> _entries = new();   // newest first
    private List<EventTrayRow> _rows = new();
    private int _shown = PageSize;
    private bool _loaded;
    private bool _isAdvanced;
    private int _unread;

    public IReadOnlyList<EventTrayRow> Rows => _rows;

    public ICommand ToggleAdvancedCommand { get; }
    public ICommand ShowOlderCommand { get; }

    // Set by the home page while the tray is on screen.
    public bool IsOpen { get; set; }

    // Raised when the unread count changes (the home page updates its button text).
    public event Action? UnreadChanged;

    public EventTrayViewModel(IPlayerActionLogService log, Func<PlayerActionLogEntry, bool> filter)
    {
        _log = log;
        _filter = filter;

        ToggleAdvancedCommand = new Command(() => { _isAdvanced = !_isAdvanced; Rebuild(); });
        ShowOlderCommand = new Command(() => { _shown += PageSize; Rebuild(); });

        _log.EntryLogged += OnEntryLogged;
    }

    // ===== LOADING =====
    public async Task OpenAsync()
    {
        IsOpen = true;
        if (_unread != 0)
        {
            _unread = 0;
            UnreadChanged?.Invoke();
        }

        if (!_loaded)
        {
            var all = await _log.GetAllEntriesAsync();
            _entries = all.Where(_filter).OrderByDescending(e => e.Timestamp).ToList();
            _shown = PageSize;
            _loaded = true;
        }
        Rebuild();
    }

    // A different character was loaded: forget everything, so the next open reads the right history.
    public void Reset()
    {
        _loaded = false;
        _entries = new List<PlayerActionLogEntry>();
        _rows = new List<EventTrayRow>();
        _shown = PageSize;
        IsOpen = false;
        if (_unread != 0)
        {
            _unread = 0;
            UnreadChanged?.Invoke();
        }
        OnPropertyChanged(string.Empty);
    }

    // May arrive on a background thread.
    private void OnEntryLogged(PlayerActionLogEntry entry)
    {
        if (!_filter(entry)) return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (_loaded && !_entries.Any(e => e.Id == entry.Id))
            {
                _entries.Insert(0, entry);
                Rebuild();
            }

            // Only events the GM imparts count as unread: the player's own actions would flood it.
            if (!IsOpen && entry.Source == ActionSource.GM)
            {
                _unread++;
                UnreadChanged?.Invoke();
            }
        });
    }

    // ===== ROWS =====
    // Builds a brand-new list and swaps it in. Never edits the list the screen is showing.
    private void Rebuild()
    {
        var rows = new List<EventTrayRow>();
        foreach (var day in _entries.Take(_shown).GroupBy(e => e.Timestamp.Date))
        {
            rows.Add(new EventTrayRow { IsHeader = true, HeaderText = DayTitle(day.Key) });
            rows.AddRange(day.Select(ToRow));
        }

        _rows = rows;
        OnPropertyChanged(string.Empty);
    }

    private EventTrayRow ToRow(PlayerActionLogEntry e) => new()
    {
        Icon = e.Icon,
        Label = e.FriendlyLabel,
        TimeText = e.Timestamp.ToString("h:mm tt"),
        Summary = e.Summary,
        IsGm = e.Source == ActionSource.GM,
        IsRuleSkating = e.IsRuleSkating,
        ShowAdvanced = _isAdvanced,
        AdvancedText = $"{e.PlayerName} ({e.CharacterName}) · player {e.PlayerId} · character {e.CharacterGuid}" +
                       (e.SessionId.HasValue ? $" · session {e.SessionId}" : string.Empty) +
                       $" · {e.Source}"
    };

    private static string DayTitle(DateTime day)
    {
        var today = DateTime.Today;
        if (day == today) return "Today";
        if (day == today.AddDays(-1)) return "Yesterday";
        return day.Year == today.Year ? day.ToString("dddd, MMM d") : day.ToString("dddd, MMM d, yyyy");
    }

    // ===== DISPLAY =====
    public bool HasNoEntries => _entries.Count == 0;
    public bool HasMore => _entries.Count > _shown;
    public string ShowOlderText => $"Show older events ({Math.Max(0, _entries.Count - _shown)} more)";
    public string AdvancedButtonText => _isAdvanced ? "Simple view" : "Advanced";
    public string CountText => _entries.Count == 1 ? "1 event" : $"{_entries.Count:N0} events";
    public string ButtonText => _unread > 0 ? $"🔔 Event Tray • {_unread}" : "🔔 Event Tray";

    // ===== INotifyPropertyChanged =====
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}