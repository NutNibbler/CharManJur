using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public class CraftOutputRow
{
    public Item Item { get; init; } = null!;
    public string Name => Item.Name;
    public string CategoryText { get; init; } = string.Empty;
    public string Description => Item.BaseDescription ?? string.Empty;
    public bool HasDescription => !string.IsNullOrWhiteSpace(Item.BaseDescription);
}

public class CraftInputRow : INotifyPropertyChanged
{
    private int _committed;

    public int CharacterItemId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int Carried { get; init; }
    public bool IsMaterial { get; init; }
    public string CategoryText { get; init; } = string.Empty;
    public string InfoText => (IsMaterial ? "Material · " : string.Empty) + $"{CategoryText} · ×{Carried} carried";

    public int Committed
    {
        get => _committed;
        set
        {
            int v = Math.Clamp(value, 0, Carried);
            if (v == _committed) return;
            _committed = v;
            OnPropertyChanged(nameof(Committed));
            OnPropertyChanged(nameof(CommittedText));
            OnPropertyChanged(nameof(HasCommitted));
            Changed?.Invoke();
        }
    }

    public string CommittedText => _committed.ToString();
    public bool HasCommitted => _committed > 0;

    public ICommand UpCommand { get; }
    public ICommand DownCommand { get; }

    public event Action? Changed;
    public event PropertyChangedEventHandler? PropertyChanged;

    public CraftInputRow()
    {
        UpCommand = new Command(() => Committed++);
        DownCommand = new Command(() => Committed--);
    }

    private void OnPropertyChanged([CallerMemberName] string name = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

// The craft screen: what to make (an item or a construct), and which of your items to commit (all consumed).
public class CraftViewModel : INotifyPropertyChanged
{
    private const int MaxConstructCapacity = 99;
    private const int MaxConstructNameLength = 40;
    private int _startingUses;

    private readonly CraftService _craft;
    private readonly SubFeatureEntry? _blueprint;      // null = sandbox crafting
    private readonly Action<string> _onDone;
    private readonly Func<Task> _close;
    private readonly Dictionary<string, FmlrSizes> _sizesByName;

    private List<Item> _allOutputs = new();
    private CraftOutputRow? _selected;
    private string _search = string.Empty;
    private string _quantityText = "1";
    private string _error = string.Empty;
    private bool _rebuilding;
    private bool _busy;
    private bool _loaded;

    private bool _constructMode;
    private string _constructName = string.Empty;
    private string _constructDescription = string.Empty;
    private string _constructSize = "Medium";
    private string _constructCapacityText = "0";

    // ===== STARTING USES =====
    public int OutputMaxUses => _selected?.Item.HasUses == true ? _selected.Item.Uses!.Value : 1;

    public int StartingUses
    {
        get => _startingUses;
        set { if (_startingUses == value) return; _startingUses = value; OnPropertyChanged(nameof(StartingUses)); }
    }

    // Offered for items with uses; a stack of more than one always starts full.
    public bool ShowStartingUses =>
        _selected?.Item.HasUses == true && (!_selected.Item.IsStackableItem || Quantity == 1);

    // A newly chosen item starts full. The maximum is announced first so the control accepts the new value.
    private void ResetStartingUses()
    {
        OnPropertyChanged(nameof(OutputMaxUses));
        _startingUses = OutputMaxUses;
        OnPropertyChanged(nameof(StartingUses));
    }

    public ObservableCollection<CraftOutputRow> Outputs { get; } = new();
    public ObservableCollection<CraftInputRow> Inputs { get; } = new();
    public List<string> ConstructSizeNames { get; }

    public ICommand QuantityUpCommand { get; }
    public ICommand QuantityDownCommand { get; }
    public ICommand ShowItemCommand { get; }
    public ICommand ShowConstructCommand { get; }
    public ICommand CraftCommand { get; }
    public ICommand CreateCustomItemCommand { get; }

    // The craft page opens the item creator; it calls back with the new item.
    public event Action<Action<Item>>? CustomItemRequested;
    public ICommand CancelCommand { get; }

    public CraftViewModel(CraftService craft, SubFeatureEntry? blueprint, Action<string> onDone, Func<Task> close)
    {
        _craft = craft;
        _blueprint = blueprint;
        _onDone = onDone;
        _close = close;

        _sizesByName = Enum.GetValues<FmlrSizes>().ToDictionary(s => EnumDisplayHelper.GetName(s), s => s);
        ConstructSizeNames = Enum.GetValues<FmlrSizes>().Select(s => EnumDisplayHelper.GetName(s)).ToList();

        QuantityUpCommand = new Command(() => SetQuantity(Quantity + 1));
        QuantityDownCommand = new Command(() => SetQuantity(Quantity - 1));
        ShowItemCommand = new Command(() => SetMode(false));
        ShowConstructCommand = new Command(() => SetMode(true));
        CraftCommand = new Command(async () => await CraftAsync());
        CreateCustomItemCommand = new Command(() => CustomItemRequested?.Invoke(AddCreatedItem));
        CancelCommand = new Command(async () => await _close());
    }

    public async Task LoadAsync()
    {
        if (_loaded) return;
        _loaded = true;

        _allOutputs = await _craft.OutputCatalogAsync();

        foreach (var input in _craft.Inputs())
        {
            var row = new CraftInputRow
            {
                CharacterItemId = input.CharacterItemId,
                Name = input.Name,
                Carried = input.Carried,
                IsMaterial = input.IsMaterial,
                CategoryText = input.CategoryText
            };
            row.Changed += () => OnPropertyChanged(string.Empty);
            Inputs.Add(row);
        }

        RebuildOutputs();
    }

    // ===== ITEM OR CONSTRUCT =====
    private void SetMode(bool construct)
    {
        if (_constructMode == construct) return;
        _constructMode = construct;
        Error = string.Empty;
        OnPropertyChanged(string.Empty);
    }

    public bool IsItemMode => !_constructMode;
    public bool IsConstructMode => _constructMode;

    // ===== ITEM OUTPUT LIST =====
    private void RebuildOutputs()
    {
        var keep = _selected?.Item;

        _rebuilding = true;
        try
        {
            Outputs.Clear();
            foreach (var item in _allOutputs)
            {
                if (_search.Length > 0 && item.Name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0) continue;
                Outputs.Add(new CraftOutputRow
                {
                    Item = item,
                    CategoryText = item.Category.HasValue ? EnumDisplayHelper.GetName(item.Category.Value) : "Other"
                });
            }
            _selected = keep == null ? null : Outputs.FirstOrDefault(r => ReferenceEquals(r.Item, keep));
        }
        finally { _rebuilding = false; }

        OnPropertyChanged(string.Empty);
    }

    public string SearchText
    {
        get => _search;
        set
        {
            string v = value?.Trim() ?? string.Empty;
            if (v == _search) return;
            _search = v;
            RebuildOutputs();
        }
    }

    public CraftOutputRow? SelectedOutput
    {
        get => _selected;
        set
        {
            if (_rebuilding || ReferenceEquals(_selected, value)) return;
            _selected = value;
            _quantityText = "1";
            Error = string.Empty;
            ResetStartingUses();
            OnPropertyChanged(string.Empty);
        }
    }

    public string QuantityText
    {
        get => _quantityText;
        set
        {
            if (_quantityText == value) return;
            _quantityText = value ?? string.Empty;
            OnPropertyChanged(string.Empty);
        }
    }

    private int Quantity => int.TryParse(_quantityText, out int q) ? Math.Clamp(q, 1, CraftService.MaxOutputQuantity) : 1;

    private void SetQuantity(int quantity)
    {
        _quantityText = Math.Clamp(quantity, 1, CraftService.MaxOutputQuantity).ToString();
        OnPropertyChanged(string.Empty);
    }

    // Called by the item creator when a custom item has been made: it joins the list and is selected.
    private void AddCreatedItem(Item item)
    {
        if (item == null) return;

        if (!_allOutputs.Any(i => ReferenceEquals(i, item) || (i.Guid != Guid.Empty && i.Guid == item.Guid)))
            _allOutputs.Add(item);
        _allOutputs = _allOutputs.OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase).ToList();

        _constructMode = false;
        _search = string.Empty;
        _selected = null;
        RebuildOutputs();

        _selected = Outputs.FirstOrDefault(r => ReferenceEquals(r.Item, item));
        _quantityText = "1";
        Error = string.Empty;
        ResetStartingUses();
        OnPropertyChanged(string.Empty);
    }

    // ===== CONSTRUCT FORM =====
    public string ConstructName
    {
        get => _constructName;
        set { if (_constructName == value) return; _constructName = value ?? string.Empty; OnPropertyChanged(string.Empty); }
    }

    public string ConstructDescription
    {
        get => _constructDescription;
        set { _constructDescription = value ?? string.Empty; }
    }

    public string SelectedConstructSize
    {
        get => _constructSize;
        set { if (value == null || _constructSize == value) return; _constructSize = value; OnPropertyChanged(string.Empty); }
    }

    public string ConstructCapacityText
    {
        get => _constructCapacityText;
        set { if (_constructCapacityText == value) return; _constructCapacityText = value ?? string.Empty; OnPropertyChanged(string.Empty); }
    }

    private bool TryCapacity(out int capacity)
        => int.TryParse(_constructCapacityText.Trim(), out capacity) && capacity >= 0 && capacity <= MaxConstructCapacity;

    private bool NameValid => !string.IsNullOrWhiteSpace(_constructName) && _constructName.Trim().Length <= MaxConstructNameLength;
    private bool ConstructValid => NameValid && _sizesByName.ContainsKey(_constructSize) && TryCapacity(out _);

    // ===== CRAFT =====
    private async Task CraftAsync()
    {
        if (_busy || !CanCraft) return;
        var page = Application.Current!.MainPage!;

        var commits = Inputs.Where(i => i.Committed > 0)
                            .Select(i => new CraftCommit(i.CharacterItemId, i.Committed))
                            .ToList();

        string recommendation = _blueprint?.Source is Blueprint bp && MaterialsCommitted < bp.BlueprintCost
            ? $"\n\nNote: the blueprint recommends {bp.BlueprintCost} materials and you committed {MaterialsCommitted}. Your GM can still allow it."
            : string.Empty;

        if (_constructMode)
        {
            string name = _constructName.Trim();
            TryCapacity(out int capacity);
            var size = _sizesByName[_constructSize];

            string message = $"Craft the construct \"{name}\" ({_constructSize}, base capacity {capacity})?\n\n" +
                             $"These are consumed:\n{ConsumeListText}{recommendation}";
            if (!await page.DisplayAlertAsync("Confirm Crafting", message, "Craft", "Cancel")) return;

            _busy = true;
            try
            {
                var result = await _craft.CraftConstructAsync(name, _constructDescription, size, capacity, commits, _blueprint);
                if (!result.Success) { Error = result.Message; return; }

                _onDone($"Crafted the construct {name}. Find it in your Familiars & Constructs panel.");
                await _close();
            }
            finally { _busy = false; }
            return;
        }

        if (_selected == null) return;
        var row = _selected;
        int quantity = Quantity;

        string itemMessage = $"Craft {quantity}x {row.Name}?\n\nThese are consumed:\n{ConsumeListText}{recommendation}";
        if (!await page.DisplayAlertAsync("Confirm Crafting", itemMessage, "Craft", "Cancel")) return;

        _busy = true;
        try
        {
            var result = await _craft.CraftItemAsync(row.Item, quantity, commits, _blueprint, ShowStartingUses ? _startingUses : (int?)null);
            if (!result.Success) { Error = result.Message; return; }

            _onDone($"Crafted {quantity}x {row.Name}.");
            await _close();
        }
        finally { _busy = false; }
    }

    // ===== DISPLAY =====
    public string Title => _blueprint != null ? $"🔨 Craft with {_blueprint.Name}" : "🛠️ Craft Without a Blueprint";
    public string Subtitle => _blueprint != null && _blueprint.HasDescription
        ? _blueprint.Description
        : "Commit any of your items to create something new. Simple things need no blueprint; your GM may ask for an Artifice roll.";

    private int? Recommended => (_blueprint?.Source as Blueprint)?.BlueprintCost;
    public int MaterialsCommitted => Inputs.Where(i => i.IsMaterial).Sum(i => i.Committed);
    public int TotalCommitted => Inputs.Sum(i => i.Committed);

    public string HintText
    {
        get
        {
            if (_blueprint == null)
                return "Everything you commit is consumed.";
            return $"Recommended: {Recommended ?? 0} material(s) (Resource items). You've committed {MaterialsCommitted}. " +
                   "This is only a suggestion; you and your GM can bypass it. Any other items you commit are consumed too.";
        }
    }
    public bool HintMet => _blueprint != null && MaterialsCommitted >= (Recommended ?? 0);
    public bool HintIsWarning => _blueprint != null && !HintMet;

    public string ConsumeListText => string.Join("\n", Inputs.Where(i => i.Committed > 0).Select(i => $"• {i.Committed}x {i.Name}"));

    public bool HasNoOutputs => Outputs.Count == 0;
    public bool HasNoInputs => Inputs.Count == 0;
    public bool HasSelection => _selected != null;
    public string SelectedName => _selected?.Name ?? string.Empty;
    public string SelectedDescription => _selected?.Description ?? string.Empty;
    public bool HasSelectedDescription => _selected?.HasDescription ?? false;

    public bool CanCraft => TotalCommitted > 0 && (_constructMode ? ConstructValid : _selected != null);

    public string StatusText
    {
        get
        {
            if (_constructMode)
            {
                if (string.IsNullOrWhiteSpace(_constructName)) return "Name your construct.";
                if (!NameValid) return $"A name can be up to {MaxConstructNameLength} characters.";
                if (!TryCapacity(out _)) return $"Base capacity must be a whole number from 0 to {MaxConstructCapacity}.";
            }
            else if (_selected == null)
            {
                return "Choose what you're making.";
            }
            if (TotalCommitted == 0) return "Commit at least one item to craft with.";
            return string.Empty;
        }
    }
    public bool HasStatus => StatusText.Length > 0;

    public string Error
    {
        get => _error;
        set { _error = value; OnPropertyChanged(nameof(Error)); OnPropertyChanged(nameof(HasError)); }
    }
    public bool HasError => _error.Length > 0;

    // ===== INotifyPropertyChanged =====
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}