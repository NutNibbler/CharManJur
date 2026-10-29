using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Helpers;
using CharManJur.Models;
using CharManJur.Services;
using CharManJur.Views.Godrick_LiveGame;

namespace CharManJur.ViewModels;

// One connected player with a tick box.
public class GmBestowRecipient : INotifyPropertyChanged
{
    private readonly Action _changed;
    private bool _isSelected;

    public GmBestowRecipient(GmPlayerRow row, bool selected, Action changed)
    {
        Row = row;
        _isSelected = selected;
        _changed = changed;
    }

    public GmPlayerRow Row { get; }
    public string Title => Row.Title;
    public string Stats => Row.Stats;

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
            _changed();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string name = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// One thing in the list of what can be given.
public sealed class GmCatalogRow
{
    public GmBestowKind Kind { get; init; }
    public Item? Item { get; init; }
    public Familiar? Familiar { get; init; }
    public SubFeatureEntry? SubFeature { get; init; }
    public ScratchPageInfo? Page { get; init; }
    public Guid CampaignId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Subtitle { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;

    public bool HasSubtitle => Subtitle.Length > 0;
    public bool HasDescription => Description.Length > 0;

    public static GmCatalogRow ForItem(Item item) => new()
    {
        Kind = GmBestowKind.Item,
        Item = item,
        Name = item.Name,
        Subtitle = string.Join(" · ", new[]
        {
            item.Category?.ToString(),
            item.ValueInChips.HasValue ? $"{item.ValueInChips} chips" : null,
            item.IsStackableItem ? "Stackable" : null
        }.Where(s => !string.IsNullOrEmpty(s))),
        Description = item.BaseDescription ?? string.Empty
    };

    public static GmCatalogRow ForFamiliar(Familiar familiar)
    {
        var row = new BestowFamiliarRow(familiar);   // the player's bestow page already words these
        return new GmCatalogRow
        {
            Kind = GmBestowKind.Familiar,
            Familiar = familiar,
            Name = row.Name,
            Subtitle = row.Subtitle,
            Description = row.Stats
        };
    }

    public static GmCatalogRow ForSubFeature(GmBestowKind kind, SubFeatureEntry entry) => new()
    {
        Kind = kind,
        SubFeature = entry,
        Name = entry.Name,
        Subtitle = entry.DetailLine,
        Description = entry.Description
    };

    public static GmCatalogRow ForPage(Guid campaignId, ScratchPageInfo page) => new()
    {
        Kind = GmBestowKind.ScratchPage,
        Page = page,
        CampaignId = campaignId,
        Name = page.Name,
        Subtitle = page.Type == ScratchPageType.Drawing ? "Drawing page" : "Text page"
    };
}

// The GM's Bestow panel: pick the players, build a basket of things, give it all at once.
public class GmBestowViewModel : INotifyPropertyChanged
{
    private const int MaxUnstackable = 99;
    private const int MaxQuantity = 99999;

    private static readonly (string Label, GmBestowKind Kind)[] Categories =
    {
        ("📦 Items", GmBestowKind.Item),
        ("🐾⚙️ Familiars & Constructs", GmBestowKind.Familiar),
        ("📜 Spells", GmBestowKind.Spell),
        ("⚡ Quips", GmBestowKind.Quip),
        ("🌀 Techniques", GmBestowKind.Technique),
        ("📐 Blueprints", GmBestowKind.Blueprint),
        ("📝 Scratch Pad Pages", GmBestowKind.ScratchPage),
    };

    private readonly IGmBestowService _service;

    private ObservableCollection<GmPlayerRow>? _players;
    private Func<Guid?>? _sessionId;
    private Func<Task>? _playersChanged;
    private Func<Guid?>? _campaignId;

    private int _categoryIndex;
    private string _searchText = string.Empty;
    private List<GmCatalogRow> _allRows = new();
    private IReadOnlyList<GmCatalogRow> _catalog = Array.Empty<GmCatalogRow>();
    private GmCatalogRow? _selected;
    private string _quantityText = "1";
    private string _noteValueText = string.Empty;
    private int _dieIndex;
    private bool _showQuantity;
    private bool _showNoteValue;
    private bool _showUsageDie;
    private string _errorText = string.Empty;
    private string _resultText = string.Empty;
    private bool _isBusy;
    private int _loadToken;

    public GmBestowViewModel(IGmBestowService service)
    {
        _service = service;

        SelectAllCommand = new Command(() => SetAll(true));
        SelectNoneCommand = new Command(() => SetAll(false));
        QuantityUpCommand = new Command(() => NudgeQuantity(+1));
        QuantityDownCommand = new Command(() => NudgeQuantity(-1));
        AddToBasketCommand = new Command(AddToBasket);
        ClearBasketCommand = new Command(() => Basket.Clear());
        BestowCommand = new Command(async () => await BestowAsync());

        Basket.CollectionChanged += (_, _) => RaiseBasketProperties();
    }

    public ObservableCollection<GmBestowRecipient> Recipients { get; } = new();
    public ObservableCollection<GmBestowLine> Basket { get; } = new();
    public List<string> CategoryLabels { get; } = Categories.Select(c => c.Label).ToList();
    public List<string> DieLabels { get; } = UsageDie.Sizes.Select(UsageDie.Label).ToList();

    public ICommand SelectAllCommand { get; }
    public ICommand SelectNoneCommand { get; }
    public ICommand QuantityUpCommand { get; }
    public ICommand QuantityDownCommand { get; }
    public ICommand AddToBasketCommand { get; }
    public ICommand ClearBasketCommand { get; }
    public ICommand BestowCommand { get; }

    // ===== PLAYERS =====
    // Wired up by the dashboard: the connected players, the session id for the log, and a callback that
    // refreshes the dashboard's player strip after their saved characters change.
    public void Attach(ObservableCollection<GmPlayerRow> players, Func<Guid?> sessionId, Func<Task> playersChanged, Func<Guid?>? campaignId = null)
    {
        _players = players;
        _sessionId = sessionId;
        _playersChanged = playersChanged;
        _campaignId = campaignId;

        players.CollectionChanged += (_, _) => MainThread.BeginInvokeOnMainThread(SyncRecipients);
        SyncRecipients();
    }

    // Rebuilds the tick boxes from the connected players, keeping whoever was ticked.
    private void SyncRecipients()
    {
        var ticked = Recipients.Where(r => r.IsSelected).Select(r => r.Row.Save.CharacterGuid).ToHashSet();

        Recipients.Clear();
        if (_players != null)
        {
            foreach (var row in _players)
                Recipients.Add(new GmBestowRecipient(row, ticked.Contains(row.Save.CharacterGuid), OnSelectionChanged));
        }

        OnPropertyChanged(nameof(HasNoRecipients));
        OnSelectionChanged();
    }

    private void SetAll(bool selected)
    {
        foreach (var r in Recipients) r.IsSelected = selected;
    }

    private void OnSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedRecipientCount));
        OnPropertyChanged(nameof(RecipientSummary));
        OnPropertyChanged(nameof(BestowButtonText));
        OnPropertyChanged(nameof(CanBestow));
    }

    public bool HasNoRecipients => Recipients.Count == 0;
    public int SelectedRecipientCount => Recipients.Count(r => r.IsSelected);
    public string RecipientSummary => SelectedRecipientCount switch
    {
        0 => "No players selected",
        1 => "1 player selected",
        var n => $"{n} players selected"
    };

    // ===== CATEGORY AND LIST =====
    public int CategoryIndex
    {
        get => _categoryIndex;
        set
        {
            if (value < 0 || value >= Categories.Length || value == _categoryIndex) return;
            _categoryIndex = value;
            _searchText = string.Empty;
            SelectedEntry = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SearchText));
            _ = LoadCatalogAsync();
        }
    }

    public string SearchText
    {
        get => _searchText;
        set
        {
            value ??= string.Empty;
            if (_searchText == value) return;
            _searchText = value;
            OnPropertyChanged();
            ApplyFilter();
        }
    }

    public IReadOnlyList<GmCatalogRow> Catalog
    {
        get => _catalog;
        private set
        {
            _catalog = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasNoCatalog));
        }
    }

    public bool HasNoCatalog => _catalog.Count == 0;

    public GmCatalogRow? SelectedEntry
    {
        get => _selected;
        set
        {
            if (ReferenceEquals(_selected, value)) return;
            _selected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedEntry));
            ConfigureOptions();
        }
    }

    public bool HasSelectedEntry => _selected != null;

    // Called when the panel opens, so newly made custom assets show up.
    public async Task RefreshAsync()
    {
        ErrorText = string.Empty;
        await LoadCatalogAsync();
    }

    private async Task LoadCatalogAsync()
    {
        int token = ++_loadToken;
        var kind = Categories[_categoryIndex].Kind;

        try
        {
            var rows = new List<GmCatalogRow>();

            switch (kind)
            {
                case GmBestowKind.Item:
                    rows.AddRange((await _service.GetItemsAsync()).Select(GmCatalogRow.ForItem));
                    break;

                case GmBestowKind.Familiar:
                    rows.AddRange((await _service.GetFamiliarsAsync()).Select(GmCatalogRow.ForFamiliar));
                    break;

                case GmBestowKind.ScratchPage:
                    if (_campaignId?.Invoke() is { } campaign)
                        rows.AddRange((await _service.GetPadPagesAsync(campaign)).Select(p => GmCatalogRow.ForPage(campaign, p)));
                    break;

                default:
                    var type = kind switch
                    {
                        GmBestowKind.Spell => SubFeatureType.Spell,
                        GmBestowKind.Quip => SubFeatureType.Quip,
                        GmBestowKind.Blueprint => SubFeatureType.Blueprint,
                        _ => SubFeatureType.Technique
                    };
                    rows.AddRange((await _service.GetSubFeaturesAsync(type)).Select(e => GmCatalogRow.ForSubFeature(kind, e)));
                    break;
            }

            if (token != _loadToken) return;   // the category changed while this was loading

            _allRows = rows.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToList();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            if (token == _loadToken) ErrorText = $"Couldn't load the list: {ex.Message}";
        }
    }

    private void ApplyFilter()
    {
        string term = _searchText.Trim();
        Catalog = term.Length == 0
            ? _allRows
            : _allRows.Where(r => r.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                               || r.Description.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

        if (_selected != null && !_catalog.Contains(_selected)) SelectedEntry = null;
    }

    // ===== OPTIONS FOR THE SELECTED ITEM =====
    public bool ShowQuantity { get => _showQuantity; private set { _showQuantity = value; OnPropertyChanged(); } }
    public bool ShowNoteValue { get => _showNoteValue; private set { _showNoteValue = value; OnPropertyChanged(); } }
    public bool ShowUsageDie { get => _showUsageDie; private set { _showUsageDie = value; OnPropertyChanged(); } }

    public string QuantityText
    {
        get => _quantityText;
        set { _quantityText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public string NoteValueText
    {
        get => _noteValueText;
        set { _noteValueText = value ?? string.Empty; OnPropertyChanged(); }
    }

    public int DieIndex
    {
        get => _dieIndex;
        set
        {
            if (value < 0 || value >= DieLabels.Count || value == _dieIndex) return;   // -1 while the picker rebuilds
            _dieIndex = value;
            OnPropertyChanged();
        }
    }

    // Each choice starts from sensible defaults: one of it, a 10-Mark Note, the item's own die.
    private void ConfigureOptions()
    {
        var item = _selected?.Item;

        ShowQuantity = item != null;
        ShowNoteValue = item?.IsValueEditable == true;
        ShowUsageDie = item?.HasUsageDie == true;

        QuantityText = "1";
        NoteValueText = CurrencyHelper.MinNoteMarks.ToString();

        _dieIndex = item?.HasUsageDie == true
            ? Math.Max(0, Array.IndexOf(UsageDie.Sizes, item.DefaultUsageDieSides))
            : 0;
        OnPropertyChanged(nameof(DieIndex));

        ErrorText = string.Empty;
    }

    private void NudgeQuantity(int delta)
    {
        int current = int.TryParse(QuantityText, out int n) ? n : 1;
        QuantityText = Math.Clamp(current + delta, 1, MaxQuantity).ToString();
    }

    // ===== BASKET =====
    public bool HasBasket => Basket.Count > 0;
    public string BasketSummary => Basket.Count == 0 ? "Nothing added yet." : $"{Basket.Count} line(s)";
    public string BestowButtonText => Basket.Count == 0
        ? "✅ Bestow"
        : SelectedRecipientCount == 1 ? "✅ Bestow to 1 player" : $"✅ Bestow to {SelectedRecipientCount} players";
    public bool CanBestow => !_isBusy && Basket.Count > 0 && SelectedRecipientCount > 0;

    private void RaiseBasketProperties()
    {
        OnPropertyChanged(nameof(HasBasket));
        OnPropertyChanged(nameof(BasketSummary));
        OnPropertyChanged(nameof(BestowButtonText));
        OnPropertyChanged(nameof(CanBestow));
    }

    public void RemoveLine(GmBestowLine line) => Basket.Remove(line);

    private void AddToBasket()
    {
        ErrorText = string.Empty;

        var entry = _selected;
        if (entry == null)
        {
            ErrorText = "Select something from the list first.";
            return;
        }

        if (entry.Kind == GmBestowKind.Item)
        {
            var item = entry.Item!;

            if (!int.TryParse(QuantityText, out int quantity) || quantity < 1 || quantity > MaxQuantity)
            {
                ErrorText = $"Enter a quantity from 1 to {MaxQuantity:N0}.";
                return;
            }
            if (!item.IsStackableItem && quantity > MaxUnstackable)
            {
                ErrorText = $"An item that doesn't stack can be given up to {MaxUnstackable} at a time.";
                return;
            }

            int? written = null;
            if (item.IsValueEditable)
            {
                if (!CurrencyHelper.TryParseNoteMarks(NoteValueText, out int chips))
                {
                    ErrorText = $"Enter a whole number of Marks from {CurrencyHelper.MinNoteMarks} to {CurrencyHelper.MaxNoteMarks:N0}.";
                    return;
                }
                written = chips;
            }

            int? die = item.HasUsageDie && _dieIndex >= 0 && _dieIndex < UsageDie.Sizes.Length
                ? UsageDie.Sizes[_dieIndex]
                : null;

            Basket.Add(new GmBestowLine
            {
                Kind = GmBestowKind.Item,
                Item = item,
                Quantity = quantity,
                WrittenValue = written,
                UsageDieSides = die
            });
        }
        else
        {
            // The same thing can't go in the basket twice. Familiars can: each one is a fresh copy.
            bool duplicate = entry.Kind switch
            {
                GmBestowKind.Familiar => false,
                GmBestowKind.ScratchPage => Basket.Any(b => b.Kind == entry.Kind && b.Page?.Id == entry.Page?.Id),
                _ => Basket.Any(b => b.Kind == entry.Kind && b.Name == entry.Name)
            };
            if (duplicate)
            {
                ErrorText = $"{entry.Name} is already in the basket.";
                return;
            }

            Basket.Add(new GmBestowLine
            {
                Kind = entry.Kind,
                Familiar = entry.Familiar,
                SubFeature = entry.SubFeature,
                Page = entry.Page,
                CampaignId = entry.CampaignId
            });
        }

        SelectedEntry = null;   // ready for the next one
    }

    // ===== BESTOWING =====
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

    public bool HasError => !string.IsNullOrEmpty(_errorText);

    public string ResultText
    {
        get => _resultText;
        private set
        {
            _resultText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasResult));
        }
    }

    public bool HasResult => !string.IsNullOrEmpty(_resultText);

    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanBestow));
        }
    }

    private async Task BestowAsync()
    {
        if (_isBusy) return;
        ErrorText = string.Empty;

        var targets = Recipients.Where(r => r.IsSelected).Select(r => r.Row.Save).ToList();
        if (targets.Count == 0)
        {
            ErrorText = "Tick at least one player.";
            return;
        }
        if (Basket.Count == 0)
        {
            ErrorText = "Add something to the basket first.";
            return;
        }

        string who = string.Join(", ", targets.Select(t => t.CharacterName));
        string what = string.Join("\n", Basket.Select(b => $"• {b.Title}"));

        // Heavy loot can push a pack over capacity. Say who it would affect before anything is given.
        var overloaded = await _service.PreviewEncumbranceAsync(targets, Basket.ToList());
        string warning = overloaded.Count == 0
            ? string.Empty
            : "\n\n⚠️ These players would become encumbered (Max HP 0 until they lighten their pack):\n"
              + string.Join("\n", overloaded.Select(o => $"• {o}"));

        bool ok = await Application.Current!.MainPage!.DisplayAlertAsync(
            "Confirm Bestow", $"Give to {who}:\n\n{what}{warning}", "Bestow", "Cancel");
        if (!ok) return;

        IsBusy = true;
        try
        {
            var outcomes = await _service.BestowAsync(targets, Basket.ToList(), _sessionId?.Invoke());
            ResultText = string.Join("\n", outcomes.Select(o => o.Summary));

            Basket.Clear();
            if (_playersChanged != null) await _playersChanged();   // the strip shows their new HP and Vigor
        }
        catch (Exception ex)
        {
            ErrorText = $"The bestow failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}