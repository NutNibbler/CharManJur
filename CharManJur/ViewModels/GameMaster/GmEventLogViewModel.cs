using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

// One line of the GM's log: a day heading or an event. A flat list, rebuilt from scratch each time,
// because editing a grouped list that is on screen crashes on Windows.
public class GmLogRow
{
    public bool IsHeader { get; init; }
    public bool IsEntry => !IsHeader;
    public string HeaderText { get; init; } = string.Empty;

    public string Icon { get; init; } = "•";
    public string Label { get; init; } = string.Empty;
    public string TimeText { get; init; } = string.Empty;
    public string PlayerLabel { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public bool IsGm { get; init; }
    public bool IsRuleSkating { get; init; }
}

// The GM's view of the session's events, with filters by player and event type and a text search.
public class GmEventLogViewModel : INotifyPropertyChanged
{
    private const int PageSize = 150;
    private const string GmKey = "gm";

    private readonly IGmEventLog _log;

    private List<GmLogRow> _rows = new();
    private List<(string Key, string Label)> _players = new();
    private List<(PlayerActionType? Type, string Label)> _types = new();
    private List<string> _playerLabels = new();
    private List<string> _typeLabels = new();

    private string _playerKey = string.Empty;          // "" = everyone, "gm" = GM actions, otherwise a character guid
    private PlayerActionType? _typeFilter;             // null = every type
    private string _search = string.Empty;
    private int _shown = PageSize;
    private int _matching;
    private int _total;

    public GmEventLogViewModel(IGmEventLog log)
    {
        _log = log;

        ShowOlderCommand = new Command(() => { _shown += PageSize; Rebuild(_log.Entries); });
        ClearFiltersCommand = new Command(ClearFilters);

        _log.Changed += () => MainThread.BeginInvokeOnMainThread(Refresh);
        Refresh();
    }

    public IReadOnlyList<GmLogRow> Rows => _rows;
    public List<string> PlayerLabels => _playerLabels;
    public List<string> TypeLabels => _typeLabels;

    public ICommand ShowOlderCommand { get; }
    public ICommand ClearFiltersCommand { get; }

    // ===== FILTERS =====
    public int PlayerIndex
    {
        get => Math.Max(0, _players.FindIndex(p => p.Key == _playerKey));
        set
        {
            if (value < 0 || value >= _players.Count) return;   // the picker reports -1 while its list is rebuilt
            string key = _players[value].Key;
            if (key == _playerKey) return;

            _playerKey = key;
            _shown = PageSize;
            Rebuild(_log.Entries);
        }
    }

    public int TypeIndex
    {
        get => Math.Max(0, _types.FindIndex(t => t.Type == _typeFilter));
        set
        {
            if (value < 0 || value >= _types.Count) return;
            var type = _types[value].Type;
            if (type == _typeFilter) return;

            _typeFilter = type;
            _shown = PageSize;
            Rebuild(_log.Entries);
        }
    }

    public string SearchText
    {
        get => _search;
        set
        {
            value ??= string.Empty;
            if (_search == value) return;

            _search = value;
            _shown = PageSize;
            Rebuild(_log.Entries);
        }
    }

    public bool HasFilters => _playerKey.Length > 0 || _typeFilter.HasValue || !string.IsNullOrWhiteSpace(_search);

    private void ClearFilters()
    {
        _playerKey = string.Empty;
        _typeFilter = null;
        _search = string.Empty;
        _shown = PageSize;

        Rebuild(_log.Entries);
        OnPropertyChanged(nameof(PlayerIndex));
        OnPropertyChanged(nameof(TypeIndex));
        OnPropertyChanged(nameof(SearchText));
    }

    private bool Matches(PlayerActionLogEntry e)
    {
        if (_playerKey.Length > 0)
        {
            if (_playerKey == GmKey)
            {
                if (e.Source != ActionSource.GM) return false;
            }
            else if (e.CharacterGuid == Guid.Empty || e.CharacterGuid.ToString("N") != _playerKey)
            {
                return false;
            }
        }

        if (_typeFilter.HasValue && e.ActionType != _typeFilter.Value) return false;

        string term = _search.Trim();
        if (term.Length > 0 &&
            !e.Summary.Contains(term, StringComparison.OrdinalIgnoreCase) &&
            !e.FriendlyLabel.Contains(term, StringComparison.OrdinalIgnoreCase))
            return false;

        return true;
    }

    // ===== REBUILDING =====
    // The log changed: refresh the lists the pickers offer, then the rows.
    private void Refresh()
    {
        var all = _log.Entries;
        RefreshOptions(all);
        Rebuild(all);
    }

    private void RefreshOptions(IReadOnlyList<PlayerActionLogEntry> all)
    {
        var players = new List<(string Key, string Label)> { (string.Empty, "All players"), (GmKey, "👑 GM actions") };
        players.AddRange(all
            .Where(e => e.CharacterGuid != Guid.Empty)
            .GroupBy(e => e.CharacterGuid)
            .Select(g => g.First())
            .OrderBy(e => e.CharacterName, StringComparer.CurrentCultureIgnoreCase)
            .Select(e => (e.CharacterGuid.ToString("N"), $"{e.CharacterName} ({e.PlayerName})")));

        var types = new List<(PlayerActionType? Type, string Label)> { (null, "All events") };
        types.AddRange(all
            .GroupBy(e => e.ActionType)
            .Select(g => ((PlayerActionType?)g.Key, g.First().FriendlyLabel))
            .OrderBy(t => t.Item2, StringComparer.CurrentCultureIgnoreCase));

        // A filter whose player or type is no longer in the log goes back to "all".
        if (_playerKey.Length > 0 && players.All(p => p.Key != _playerKey)) _playerKey = string.Empty;
        if (_typeFilter.HasValue && types.All(t => t.Type != _typeFilter)) _typeFilter = null;

        _players = players;
        _types = types;
        _playerLabels = players.Select(p => p.Label).ToList();
        _typeLabels = types.Select(t => t.Label).ToList();

        OnPropertyChanged(nameof(PlayerLabels));
        OnPropertyChanged(nameof(TypeLabels));
        OnPropertyChanged(nameof(PlayerIndex));   // puts the selections back after the lists change
        OnPropertyChanged(nameof(TypeIndex));
    }

    // Builds a brand-new list and swaps it in. Never edits the list the screen is showing.
    private void Rebuild(IReadOnlyList<PlayerActionLogEntry> all)
    {
        var matching = all.Where(Matches).ToList();   // already newest first
        _total = all.Count;
        _matching = matching.Count;

        var rows = new List<GmLogRow>();
        foreach (var day in matching.Take(_shown).GroupBy(e => e.Timestamp.Date))
        {
            rows.Add(new GmLogRow { IsHeader = true, HeaderText = DayTitle(day.Key) });
            rows.AddRange(day.Select(ToRow));
        }
        _rows = rows;

        OnPropertyChanged(nameof(Rows));
        OnPropertyChanged(nameof(CountText));
        OnPropertyChanged(nameof(EmptyText));
        OnPropertyChanged(nameof(HasNoRows));
        OnPropertyChanged(nameof(HasMore));
        OnPropertyChanged(nameof(ShowOlderText));
        OnPropertyChanged(nameof(HasFilters));
        OnPropertyChanged(nameof(ArchiveText));
        OnPropertyChanged(nameof(HasArchive));
    }

    private static GmLogRow ToRow(PlayerActionLogEntry e) => new()
    {
        Icon = e.Icon,
        Label = e.FriendlyLabel,
        TimeText = e.Timestamp.ToString("h:mm tt"),
        PlayerLabel = e.CharacterGuid == Guid.Empty ? "GM" : $"{e.CharacterName} ({e.PlayerName})",
        Summary = e.Summary,
        IsGm = e.Source == ActionSource.GM,
        IsRuleSkating = e.IsRuleSkating
    };

    private static string DayTitle(DateTime day)
    {
        var today = DateTime.Today;
        if (day == today) return "Today";
        if (day == today.AddDays(-1)) return "Yesterday";
        return day.Year == today.Year ? day.ToString("dddd, MMM d") : day.ToString("dddd, MMM d, yyyy");
    }

    // ===== DISPLAY =====
    public bool HasNoRows => _rows.Count == 0;
    public string EmptyText => _total == 0 ? "No events yet." : "No events match the filters.";
    public bool HasMore => _matching > _shown;
    public string ShowOlderText => $"Show older events ({Math.Max(0, _matching - _shown)} more)";

    public string CountText => !HasFilters || _matching == _total
        ? (_total == 1 ? "1 event" : $"{_total:N0} events")
        : $"{_matching:N0} of {_total:N0} events";

    public bool HasArchive => _log.ArchivedCount > 0;
    public string ArchiveText => $"{_log.ArchivedCount:N0} older event{(_log.ArchivedCount == 1 ? " is" : "s are")} archived on disk. The log keeps the latest {_log.ActiveLimit}.";

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}