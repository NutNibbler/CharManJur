using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels;

// One editable month or weekday name.
public class NameRow
{
    private string _name;
    private readonly Action _changed;

    public NameRow(string name, Action changed)
    {
        _name = name;
        _changed = changed;
    }

    public string Name
    {
        get => _name;
        set
        {
            if (_name == value) return;
            _name = value ?? string.Empty;
            _changed();
        }
    }
}

// One NPC profile field. The list is fixed; only Kin, Profession and Accent can be renamed.
public class NpcFieldRow
{
    public NpcFieldRow(NpcFieldDefinition definition) => Definition = definition;

    public NpcFieldDefinition Definition { get; }

    public string Label
    {
        get => Definition.Label;
        set { if (Definition.CanRename) Definition.Label = value ?? string.Empty; }
    }

    public bool CanRename => Definition.CanRename;
    public bool IsFixed => !Definition.CanRename;
    public string Hint => Definition.Hint;
}

// One faction. Edits go straight into the Faction it wraps.
public class FactionRow
{
    public FactionRow(Faction faction) => Faction = faction;

    public Faction Faction { get; }

    public string Name
    {
        get => Faction.Name;
        set => Faction.Name = value ?? string.Empty;
    }

    public string Description
    {
        get => Faction.Description;
        set => Faction.Description = value ?? string.Empty;
    }
}

public class CampaignCreatorViewModel : INotifyPropertyChanged
{
    private readonly ICampaignStore _store;

    private Campaign _campaign = new();
    private bool _isNew = true;
    private string _name = string.Empty;
    private string _description = string.Empty;
    private int _policyIndex = (int)CustomAssetPolicy.PromptMerge;
    private string _packName = string.Empty;
    private bool _showYear = true;
    private string _startYearText = "443";
    private double _monthCount;
    private double _daysPerWeek;
    private double _weeksPerMonth;
    private double _hoursPerDay;
    private int _startDayIndex;
    private List<string> _startDayItems = new();
    private string _summary = string.Empty;
    private string _errorText = string.Empty;

    public ObservableCollection<NameRow> MonthRows { get; } = new();
    public ObservableCollection<NameRow> DayRows { get; } = new();
    public ObservableCollection<NpcFieldRow> FieldRows { get; } = new();
    public ObservableCollection<FactionRow> FactionRows { get; } = new();

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand ResetCalendarCommand { get; }
    public ICommand ResetFieldNamesCommand { get; }
    public ICommand AddFactionCommand { get; }
    public ICommand RemoveFactionCommand { get; }

    public CampaignCreatorViewModel(ICampaignStore store)
    {
        _store = store;

        SaveCommand = new Command(async () => await SaveAsync());
        CancelCommand = new Command(async () => await CancelAsync());
        ResetCalendarCommand = new Command(ResetCalendar);
        ResetFieldNamesCommand = new Command(ResetFieldNames);
        AddFactionCommand = new Command(AddFaction);
        RemoveFactionCommand = new Command<FactionRow>(RemoveFaction);
    }

    // ===== PAGE STATE =====
    public bool IsNew
    {
        get => _isNew;
        private set
        {
            _isNew = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(PageTitle));
        }
    }

    public string PageTitle => IsNew ? "Create Campaign" : "Edit Campaign";

    public string Name
    {
        get => _name;
        set { _name = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string Description
    {
        get => _description;
        set { _description = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string ErrorText
    {
        get => _errorText;
        private set
        {
            _errorText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasError));
        }
    }

    public bool HasError => !string.IsNullOrEmpty(ErrorText);

    public string CalendarSummary
    {
        get => _summary;
        private set { _summary = value; OnPropertyChanged(); }
    }

    public bool HasNoFactions => FactionRows.Count == 0;

    // ===== PLAYERS AND ASSETS =====
    public List<string> PolicyLabels { get; } = AssetPolicyInfo.Labels.ToList();

    public int AssetPolicyIndex
    {
        get => _policyIndex;
        set
        {
            if (value < 0 || value >= PolicyLabels.Count || value == _policyIndex) return;   // -1 while the picker rebuilds
            _policyIndex = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AssetPolicyDescription));
        }
    }

    public string AssetPolicyDescription => AssetPolicyInfo.DescriptionFor((CustomAssetPolicy)_policyIndex);

    public string AssetPackName
    {
        get => _packName;
        set { _packName = value ?? string.Empty; OnPropertyChanged(); }
    }

    // ===== CALENDAR COUNTS (steppers) =====
    // Months and weekdays resize their name lists: names that exist are kept, new slots get "Month 13" etc.
    public double MonthCountValue
    {
        get => _monthCount;
        set
        {
            int count = (int)Math.Round(value);
            ResizeRows(MonthRows, count, "Month", RefreshSummary);
            _monthCount = count;
            OnPropertyChanged();
            OnPropertyChanged(nameof(MonthCountText));
            RefreshSummary();
        }
    }

    public double DaysPerWeekValue
    {
        get => _daysPerWeek;
        set
        {
            int count = (int)Math.Round(value);
            ResizeRows(DayRows, count, "Day", OnDayNameChanged);
            _daysPerWeek = count;
            RefreshStartDayItems();
            OnPropertyChanged();
            OnPropertyChanged(nameof(DaysPerWeekText));
            RefreshSummary();
        }
    }

    public double WeeksPerMonthValue
    {
        get => _weeksPerMonth;
        set
        {
            _weeksPerMonth = Math.Round(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(WeeksPerMonthText));
            RefreshSummary();
        }
    }

    public double HoursPerDayValue
    {
        get => _hoursPerDay;
        set
        {
            _hoursPerDay = Math.Round(value);
            OnPropertyChanged();
            OnPropertyChanged(nameof(HoursPerDayText));
            RefreshSummary();
        }
    }

    public string MonthCountText => ((int)_monthCount).ToString();
    public string DaysPerWeekText => ((int)_daysPerWeek).ToString();
    public string WeeksPerMonthText => ((int)_weeksPerMonth).ToString();
    public string HoursPerDayText => ((int)_hoursPerDay).ToString();

    public bool ShowYear
    {
        get => _showYear;
        set { _showYear = value; OnPropertyChanged(); RefreshSummary(); }
    }

    public string StartYearText
    {
        get => _startYearText;
        set { _startYearText = value ?? string.Empty; OnPropertyChanged(); RefreshSummary(); }
    }

    // ===== STARTING DAY (the weekday day 1 falls on) =====
    // The picker's list is rebuilt whenever a weekday is renamed, added or removed.
    public List<string> StartDayItems
    {
        get => _startDayItems;
        private set { _startDayItems = value; OnPropertyChanged(); }
    }

    public int StartDayIndex
    {
        get => _startDayIndex;
        set
        {
            if (value < 0 || value >= DayRows.Count) return;   // the picker reports -1 while its list is rebuilt
            if (_startDayIndex == value) return;
            _startDayIndex = value;
            OnPropertyChanged();
            RefreshSummary();
        }
    }

    private void RefreshStartDayItems()
    {
        _startDayIndex = Math.Clamp(_startDayIndex, 0, Math.Max(0, DayRows.Count - 1));
        StartDayItems = DayRows
            .Select(r => string.IsNullOrWhiteSpace(r.Name) ? "(unnamed)" : r.Name.Trim())
            .ToList();
        OnPropertyChanged(nameof(StartDayIndex));   // puts the selection back after the list changes
    }

    private void OnDayNameChanged()
    {
        RefreshStartDayItems();
        RefreshSummary();
    }

    // ===== LOADING =====
    // Called with a campaign id to edit it, or anything else ("new") to start a fresh campaign.
    public async Task LoadAsync(string? campaignIdText)
    {
        Campaign? campaign = null;
        if (Guid.TryParse(campaignIdText, out var id))
            campaign = await _store.LoadCampaignAsync(id);

        IsNew = campaign == null;
        Populate(campaign ?? new Campaign());
    }

    private void Populate(Campaign campaign)
    {
        _campaign = campaign;
        Name = campaign.Name;
        Description = campaign.Description;
        ErrorText = string.Empty;

        _policyIndex = (int)campaign.AssetPolicy;
        _packName = campaign.AssetPackName;
        OnPropertyChanged(nameof(AssetPolicyIndex));
        OnPropertyChanged(nameof(AssetPolicyDescription));
        OnPropertyChanged(nameof(AssetPackName));

        FieldRows.Clear();
        foreach (var field in campaign.NpcFields) FieldRows.Add(new NpcFieldRow(field));

        FactionRows.Clear();
        foreach (var faction in campaign.Factions) FactionRows.Add(new FactionRow(faction));
        OnPropertyChanged(nameof(HasNoFactions));

        ApplyCalendar(campaign.Calendar);
    }

    // Sets every calendar control from a config without going through the resizing setters.
    private void ApplyCalendar(CalendarConfig calendar)
    {
        FillNames(MonthRows, calendar.MonthNames, RefreshSummary);
        FillNames(DayRows, calendar.DayNames, OnDayNameChanged);

        _monthCount = MonthRows.Count;
        _daysPerWeek = DayRows.Count;
        _weeksPerMonth = calendar.WeeksPerMonth;
        _hoursPerDay = calendar.HoursPerDay;
        _showYear = calendar.ShowYear;
        _startYearText = calendar.StartYear.ToString();
        _startDayIndex = Math.Clamp(calendar.StartDayOfWeek, 0, Math.Max(0, DayRows.Count - 1));

        OnPropertyChanged(nameof(MonthCountValue));
        OnPropertyChanged(nameof(MonthCountText));
        OnPropertyChanged(nameof(DaysPerWeekValue));
        OnPropertyChanged(nameof(DaysPerWeekText));
        OnPropertyChanged(nameof(WeeksPerMonthValue));
        OnPropertyChanged(nameof(WeeksPerMonthText));
        OnPropertyChanged(nameof(HoursPerDayValue));
        OnPropertyChanged(nameof(HoursPerDayText));
        OnPropertyChanged(nameof(ShowYear));
        OnPropertyChanged(nameof(StartYearText));
        RefreshStartDayItems();

        RefreshSummary();
    }

    private static void FillNames(ObservableCollection<NameRow> rows, IEnumerable<string> names, Action changed)
    {
        rows.Clear();
        foreach (string name in names) rows.Add(new NameRow(name, changed));
    }

    private static void ResizeRows(ObservableCollection<NameRow> rows, int count, string prefix, Action changed)
    {
        while (rows.Count > count) rows.RemoveAt(rows.Count - 1);
        while (rows.Count < count) rows.Add(new NameRow($"{prefix} {rows.Count + 1}", changed));
    }

    // ===== CALENDAR PREVIEW =====
    private CalendarConfig DraftCalendar() => new()
    {
        MonthNames = MonthRows.Select(r => r.Name.Trim()).ToList(),
        DayNames = DayRows.Select(r => r.Name.Trim()).ToList(),
        WeeksPerMonth = (int)_weeksPerMonth,
        HoursPerDay = (int)_hoursPerDay,
        StartDayOfWeek = Math.Clamp(_startDayIndex, 0, Math.Max(0, DayRows.Count - 1)),
        ShowYear = _showYear,
        StartYear = int.TryParse(_startYearText, out int year) ? year : 443
    };

    private void RefreshSummary()
    {
        var c = DraftCalendar();
        CalendarSummary =
            $"{c.DaysPerYear} days a year ({c.MonthsPerYear} months × {c.WeeksPerMonth} weeks × {c.DaysPerWeek} days) · {c.HoursPerDay} hours a day\n" +
            $"Day 1 is {c.DateFor(1).Long}";
    }

    // ===== COMMANDS =====
    private void ResetCalendar() => ApplyCalendar(new CalendarConfig());

    // Puts the renamable fields (Kin, Profession, Accent) back to their default names.
    private void ResetFieldNames()
    {
        FieldRows.Clear();
        foreach (var field in NpcFieldDefaults.Create()) FieldRows.Add(new NpcFieldRow(field));
    }

    private void AddFaction()
    {
        FactionRows.Add(new FactionRow(new Faction { Name = $"Faction {FactionRows.Count + 1}" }));
        OnPropertyChanged(nameof(HasNoFactions));
    }

    private void RemoveFaction(FactionRow? row)
    {
        if (row == null) return;
        FactionRows.Remove(row);
        OnPropertyChanged(nameof(HasNoFactions));
    }

    private async Task SaveAsync()
    {
        ErrorText = string.Empty;

        if (_showYear && !int.TryParse(_startYearText, out _))
        {
            ErrorText = "The starting year must be a whole number.";
            return;
        }

        _campaign.Name = Name;
        _campaign.Description = Description;
        _campaign.Calendar = DraftCalendar();
        _campaign.NpcFields = FieldRows.Select(r => r.Definition).ToList();
        _campaign.Factions = FactionRows.Select(r => r.Faction).ToList();
        _campaign.AssetPolicy = (CustomAssetPolicy)_policyIndex;
        _campaign.AssetPackName = AssetPackName;
        _campaign.LastOpenedUtc = DateTime.UtcNow;

        string? error = await _store.SaveCampaignAsync(_campaign);
        if (error != null)
        {
            ErrorText = error;
            return;
        }

        await Shell.Current.GoToAsync("///CampaignListPage");
    }

    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync(IsNew ? "///MainPage" : "///CampaignListPage");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}