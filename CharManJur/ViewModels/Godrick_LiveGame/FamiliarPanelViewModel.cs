using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows.Input;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public class FamiliarPackItem
{
    public int ItemId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string SizeLabel { get; set; } = string.Empty;
    public string UsesText { get; set; } = string.Empty;
    public ICommand? TakeCommand { get; set; }
    public bool HasSizeLabel => !string.IsNullOrEmpty(SizeLabel);
    public bool HasUses => !string.IsNullOrEmpty(UsesText);
    public bool ShowQuantity => Quantity > 1;
    public string QuantityText => $"×{Quantity}";
    public bool IsEquipped { get; set; }
    public string EquipText => IsEquipped ? "Unequip" : "Equip";
    public bool CanEquip { get; set; }
    public ICommand? EquipCommand { get; set; }
}

public class FamiliarPanelViewModel : INotifyPropertyChanged
{
    private readonly ICharAttribDataService _svc;
    private Familiar? _selected;
    private bool _refreshing;

    public ObservableCollection<Familiar> Familiars { get; } = new();
    public ObservableCollection<FamiliarPackItem> PackItems { get; } = new();

    public ICommand AddFatigueCommand { get; }
    public ICommand RemoveFatigueCommand { get; }
    public ICommand BestowFamiliarCommand { get; }
    public ICommand RenameCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand StatUpCommand { get; }
    public ICommand StatDownCommand { get; }
    public ICommand HpUpCommand { get; }
    public ICommand HpDownCommand { get; }
    public ICommand RestoreAllCommand { get; }
    public event Action? BestowFamiliarRequested;
    public event Action? CharacterInventoryChanged;
    public FamiliarPanelViewModel(ICharAttribDataService svc)
    {
        _svc = svc;
        AddFatigueCommand = new Command(() => ChangeFatigue(+1));
        RemoveFatigueCommand = new Command(() => ChangeFatigue(-1));
        BestowFamiliarCommand = new Command(() => BestowFamiliarRequested?.Invoke());
        RenameCommand = new Command(async () => await RenameAsync());
        RemoveCommand = new Command(async () => await RemoveAsync());
        StatUpCommand = new Command<string>(async s => await ChangeStatAsync(s, +1));
        StatDownCommand = new Command<string>(async s => await ChangeStatAsync(s, -1));
        HpUpCommand = new Command(() => ChangeHp(+1));
        HpDownCommand = new Command(() => ChangeHp(-1));
        RestoreAllCommand = new Command(RestoreAll);
    }

    // ===== SELECTION =====
    public Familiar? SelectedFamiliar
    {
        get => _selected;
        set
        {
            if (_refreshing || ReferenceEquals(_selected, value)) return;
            _selected = value;
            RebuildPack();
            OnPropertyChanged(string.Empty);
        }
    }

    public void Refresh()
    {
        _refreshing = true;
        try
        {
            var keep = _selected?.InstanceId;
            Familiars.Clear();
            foreach (var f in _svc.AcquiredFamiliars) Familiars.Add(f);
            _selected = Familiars.FirstOrDefault(f => f.InstanceId == keep) ?? Familiars.FirstOrDefault();
        }
        finally { _refreshing = false; }

        RebuildPack();
        OnPropertyChanged(string.Empty);
    }

    // After bestowing, jump to the newest familiar.
    public void SelectLast() => SelectedFamiliar = Familiars.LastOrDefault();

    private void ChangeFatigue(int delta)
    {
        if (_selected == null || _selected.IsDead) return;
        _svc.AdjustFamiliarFatigue(_selected.InstanceId, delta);
        OnPropertyChanged(string.Empty);
    }

    private async Task ChangeStatAsync(string? statName, int delta)
    {
        if (_selected == null || _selected.IsDead) return;
        if (!Enum.TryParse<FamiliarStat>(statName, out var stat)) return;

        // Guard against a mis-tap killing the familiar.
        if (stat == FamiliarStat.Vigor && delta < 0 && _selected.CurrentVigor + delta <= 0)
        {
            bool ok = await Application.Current!.MainPage!.DisplayAlertAsync(
                "Familiar Death",
                $"Reducing Vigor to 0 will kill {_selected.DisplayName}. Continue?",
                "Yes, it dies", "Cancel");
            if (!ok) return;
        }

        _svc.AdjustFamiliarAbility(_selected.InstanceId, stat, delta);
        OnPropertyChanged(string.Empty);
    }

    private void ChangeHp(int delta)
    {
        if (_selected == null || _selected.IsDead) return;
        _svc.AdjustFamiliarHitpoints(_selected.InstanceId, delta);
        OnPropertyChanged(string.Empty);
    }

    private void RestoreAll()
    {
        if (_selected == null || _selected.IsDead) return;
        _svc.RestoreFamiliar(_selected.InstanceId);
        OnPropertyChanged(string.Empty);
    }

    private async Task RenameAsync()
    {
        if (_selected == null || _selected.IsFormPack) return;

        bool construct = _selected.FmlrIntelligence == FmlrIntelligences.Inanimate;
        string? result = await Application.Current!.MainPage!.DisplayPromptAsync(
            construct ? "Name Construct" : "Name Familiar",
            $"Give your {_selected.FmlrName} a name (leave blank to clear):",
            "Save",
            "Cancel",
            initialValue: _selected.FmlrPlayerName ?? string.Empty,
            maxLength: 40);

        if (result == null) return;   // cancelled

        _svc.SetFamiliarGivenName(_selected.InstanceId, result);
        Refresh();   // rebuilds the list so the picker text updates
    }

    private void ToggleEquip(int itemId)
    {
        if (_selected == null) return;
        var rec = _selected.Inventory.FirstOrDefault(i => i.Id == itemId);
        if (rec == null) return;
        if (_svc.SetFamiliarItemEquipped(_selected.InstanceId, itemId, !rec.IsEquipped))
            Refresh();
    }

    private async Task RemoveAsync()
    {
        if (_selected == null || _selected.IsFormPack) return;

        var familiar = _selected;
        bool construct = familiar.FmlrIntelligence == FmlrIntelligences.Inanimate;
        int itemCount = familiar.Inventory?.Count ?? 0;
        string warning = itemCount > 0
            ? $"\n\nIts {itemCount} carried item record(s) will be lost."
            : string.Empty;

        string question = familiar.IsDead
            ? $"{familiar.DisplayName} is dead. Remove it from this character?{warning}"
            : construct
                ? $"Remove {familiar.DisplayName} from this character? It will be gone for good.{warning}"
                : $"Release {familiar.DisplayName} from this character?{warning}";

        bool confirmed = await Application.Current!.MainPage!.DisplayAlertAsync(
            familiar.IsDead ? "Remove Dead Familiar" : construct ? "Remove Construct" : "Release Familiar",
            question,
            familiar.IsDead || construct ? "Remove" : "Release", "Cancel");

        if (!confirmed) return;

        _svc.RemoveFamiliar(familiar.InstanceId);
        _selected = null;
        Refresh();
    }

    private void RebuildPack()
    {
        PackItems.Clear();
        if (_selected == null) return;

        foreach (var item in _selected.Inventory.Where(i => !i.IsDropped)
                                        .OrderByDescending(i => i.IsEquipped))
        {
            var size = item.Template == null
                ? ItemSize.Regular
                : InventorySlotCalculator.EffectiveSize(item.Template, item.Quantity);

            var row = new FamiliarPackItem
            {
                ItemId = item.Id,
                Name = item.DisplayName,
                Quantity = item.Quantity,
                IsEquipped = item.IsEquipped,
                CanEquip = !_selected.IsDead,
                EquipCommand = new Command(() => ToggleEquip(item.Id)),
                SizeLabel = size switch
                {
                    ItemSize.Petty => "Petty",
                    ItemSize.Bulky => "Bulky",
                    _ => string.Empty
                },
                UsesText = item.Template?.HasUses == true
                    ? $"{item.RemainingUses}/{item.Template.Uses!.Value} uses"
                    : string.Empty
            };
            row.TakeCommand = new Command(async () => await TakeAsync(row));
            PackItems.Add(row);
        }
    }

    private async Task TakeAsync(FamiliarPackItem row)
    {
        if (_selected == null) return;
        var page = Application.Current!.MainPage!;

        if (_svc.IsPackDropped)
        {
            await page.DisplayAlertAsync("Pack Dropped", "Pick up your pack before taking items.", "OK");
            return;
        }

        int quantity = row.Quantity;
        if (row.Quantity > 1)
        {
            string? answer = await page.DisplayPromptAsync(
                "Take Items", $"How many {row.Name} (1–{row.Quantity})?",
                "Take", "Cancel", initialValue: row.Quantity.ToString(), keyboard: Keyboard.Numeric);
            if (answer == null || !int.TryParse(answer, out quantity) || quantity < 1) return;
            quantity = Math.Min(quantity, row.Quantity);
        }

        if (_svc.TakeItemFromFamiliar(_selected.InstanceId, row.ItemId, quantity))
        {
            RebuildPack();
            OnPropertyChanged(string.Empty);
            CharacterInventoryChanged?.Invoke();
        }
    }

    // ===== DISPLAY =====
    public bool HasFamiliar => _selected != null;
    public bool HasNoFamiliar => _selected == null;
    public bool HasMultiple => Familiars.Count > 1;

    // A Shifter form's pack can't be named or removed by hand: it goes away when the form ends.
    public bool IsFormPack => _selected?.IsFormPack ?? false;
    public bool CanRenameOrRemove => _selected != null && !IsFormPack;

    public string Name => _selected == null ? string.Empty
    : _selected.HasGivenName ? _selected.FmlrPlayerName! : (_selected.FmlrName ?? string.Empty);

    public string Subtitle => _selected == null ? string.Empty
        : string.Join(" · ", new[]
        {
            _selected.HasGivenName ? _selected.FmlrName : null,   // show the base name under a given name
            _selected.FmlrSize.ToString(),
            _selected.FmlrClass.HasValue ? _selected.FmlrClass.Value.ToString() : null,
            _selected.FmlrIntelligence.HasValue ? SpaceOut(_selected.FmlrIntelligence.Value.ToString()) : null
        }.Where(s => !string.IsNullOrEmpty(s)));

    public string Description => _selected?.FmlrDescription ?? string.Empty;
    public bool HasDescription => !string.IsNullOrWhiteSpace(_selected?.FmlrDescription);
    public string HpDisplay => _selected == null ? string.Empty : $"{_selected.CurrentHP} / {_selected.HP}";

    public string VigorDisplay => StatText(_selected?.CurrentVigor, _selected?.StatVigor, _selected?.VigorModifier);
    public string AgilityDisplay => StatText(_selected?.CurrentAgility, _selected?.StatAgility, _selected?.AgilityModifier);
    public string MindDisplay => StatText(_selected?.CurrentMind, _selected?.StatMind, _selected?.MindModifier);
    public string SpiritDisplay => StatText(_selected?.CurrentSpirit, _selected?.StatSpirit, _selected?.SpiritModifier);

    public bool IsDead => _selected?.IsDead ?? false;
    public bool IsAlive => _selected != null && !_selected.IsDead;
    // Inanimate familiars (constructs) have no stats, HP, fatigue, natural weapon or abilities to show.
    public bool IsInanimate => _selected?.FmlrIntelligence == FmlrIntelligences.Inanimate;
    public bool ShowLifeStats => _selected != null && !IsInanimate;

    public string FatigueDisplay => _selected == null ? string.Empty
        : $"{_selected.FatigueInBar} / {_selected.FatigueBarMax}";
    public bool HasFatigueOverflow => (_selected?.FatigueOverflow ?? 0) > 0;

    public string WeaponSummary => _selected?.WeaponSummary ?? string.Empty;

    public string AbilitiesText => _selected?.Abilities is { Count: > 0 } a
        ? string.Join("\n", a.Select(x => "• " + x))
        : "None";

    public bool HasPackItems => PackItems.Count > 0;
    public string PackDisplay => _selected == null ? string.Empty
        : $"{_selected.InventorySlotsUsed} / {_selected.InventorySlotsAvailable}";
    public bool IsEncumbered => _selected?.IsEncumbered ?? false;
    // Mini dashboard: "used / available", or a dash when the familiar has no pack.
    public string PackHeadline => _selected == null ? string.Empty : PackDisplay;
    public Color PackHeadlineColor => IsEncumbered ? Color.FromArgb("#e74c3c") : Colors.White;
    public bool HasInventory => _selected != null;
    public bool ShowNoInventory => _selected != null && _selected.InventorySlotsTotal == 0
                                   && !_selected.Inventory.Any(i => !i.IsDropped);
    public string FatigueOverflowText => $"+{_selected?.FatigueInInventory ?? 0} fatigue in pack";
    private static string Signed(int v) => v >= 0 ? $"+{v}" : v.ToString();
    private static string StatText(int? current, int? max, int? mod)
    {
        if (current == null || max == null || mod == null) return string.Empty;
        return current == max
            ? $"{current} ({Signed(mod.Value)})"
            : $"{current}/{max} ({Signed(mod.Value)})";
    }
    private static string SpaceOut(string s) => Regex.Replace(s, "(?<=[a-z])(?=[A-Z])", " ");

    // ===== INotifyPropertyChanged =====
    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}