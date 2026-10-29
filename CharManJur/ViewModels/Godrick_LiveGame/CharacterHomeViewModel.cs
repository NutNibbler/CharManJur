using CharManJur.Models;
using CharManJur.Services;
using CharManJur.Views.Godrick_LiveGame;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CharManJur.ViewModels;
using CharManJur.Views;
using Microsoft.Extensions.DependencyInjection;

namespace CharManJur.ViewModels.Godrick_LiveGame;

public partial class CharacterHomeViewModel : INotifyPropertyChanged
{
    private readonly ICharAttribDataService _charDataService;
    private readonly ICharacterPersistenceService _persistenceService;
    private readonly IItemDataService _itemDataService;
    private readonly IPlayerActionLogService _actionLogService;
    private readonly UsesChangeLogger _usesLog;

    private ObservableCollection<InventoryItemDisplay> _inventoryItems = new();
    private InventoryItemDisplay? _selectedInventoryItem;
    public ObservableCollection<CharacterEffect> ActiveEffects { get; } = new();
    public ICommand RemoveEffectCommand { get; }

    // ===== EQUIPMENT BACKING FIELDS =====
    private string _equippedArmor = "None";
    private string _equippedHands = "0 / 0";
    private string _equippedBelt = "0 / 4";

    // ===== DROP MODE =====
    private bool _isDropModeActive = false;

    public CharacterHomeViewModel(
        ICharAttribDataService charDataService,
        ICharacterPersistenceService persistenceService,
        IItemDataService itemDataService,
        IPlayerActionLogService actionLogService)
    {
        _charDataService = charDataService;
        _persistenceService = persistenceService;
        _itemDataService = itemDataService;
        _actionLogService = actionLogService;
        _usesLog = new UsesChangeLogger(charDataService, actionLogService);
        FavourTracker = new FavourTracker(charDataService, actionLogService);
        FavourTracker.Changed += () => OnPropertyChanged(nameof(FavourAmount));
        FavourUpCommand = new Command(() => FavourTracker.Adjust(+1));
        FavourDownCommand = new Command(() => FavourTracker.Adjust(-1));

        // ===== NAVIGATION COMMANDS =====
        NavigateToFeaturesCommand = new Command(async () => await Shell.Current.GoToAsync("///FeaturesPage"));
        OpenOptionsCommand = new Command(async () => await OnOpenOptions());
        CurrencyDisplaySettings.Changed += UpdateCurrencyDisplay;   // a preference change refreshes the dashboard right away

        // ===== INVENTORY INTERACTION COMMANDS =====
        InteractCommand = new Command(OnInteract);
        ExpendSelectedCommand = new Command(OnExpendSelected);
        IncrementUsesCommand = new Command(OnIncrementUses);
        DecrementUsesCommand = new Command(OnDecrementUses);
        BestowCommand = new Command(OnBestow);
        RecoverItemCommand = new Command(OnRecoverItem);
        ToggleDropModeCommand = new Command(OnToggleDropMode);
        EditNoteCommand = new Command(OnEditNote);
        EquipSelectedToHandCommand = new Command(OnEquipSelectedToHand);
        EquipSelectedToBeltCommand = new Command(OnEquipSelectedToBelt);
        HandToFamiliarCommand = new Command(OnHandToFamiliar);
        UnequipSelectedCommand = new Command(OnUnequipSelected);
        EquipSelectedAsArmorCommand = new Command(OnEquipSelectedAsArmor);
        OpenBioCommand = new Command(async () => await OnOpenBio());
        OpenUpkeepCommand = new Command(async () => await OnOpenUpkeep());
        OpenRollDieCommand = new Command(async () => await OnOpenRollDie());
        OpenEndOfCombatCommand = new Command(async () => await OnOpenEndOfCombat());
        OpenApplyDamageCommand = new Command(async () => await OnOpenApplyDamage());
        OpenApplyEffectCommand = new Command(async () => await OnOpenApplyEffect());
        RemoveEffectCommand = new Command<CharacterEffect>(OnRemoveEffect);
        TogglePackCommand = new Command(async () => await OnTogglePack());
        ExplainEncumbranceCommand = new Command(async () => await OnExplainEncumbrance());
        ExplainDeprivationCommand = new Command(async () => await DailyNeedsActions.ExplainDeprivationAsync(_charDataService));
    }

    private async void OnRecoverItem()
    {
        var recoverPage = new RecoverItemPage(_charDataService, async () =>
        {
            await LoadCharacterDataAsync();
        });
        await Shell.Current.Navigation.PushModalAsync(recoverPage);
    }

    // ===== HEADER PROPERTIES =====
    public string CharacterName => _charDataService.CharacterName;
    public string PlayerName => _charDataService.PlayerName;

    // ===== RACE / LANGUAGE INFO =====
    public string CharacterRace => _charDataService.CharacterRace;
    public string RaceFeatureName => _charDataService.CharacterRaceFeatureName;
    public string RaceFeatureDescription => _charDataService.CharacterRaceFeatureDescription;
    public bool HasRaceInfo => !string.IsNullOrWhiteSpace(CharacterRace);
    public bool HasRaceFeature => !string.IsNullOrWhiteSpace(RaceFeatureName);
    public bool HasRaceFeatureDescription => !string.IsNullOrWhiteSpace(RaceFeatureDescription);

    public string LanguagesText => _charDataService.SelectedLanguages.Count == 0
        ? string.Empty
        : "Languages: " + string.Join(", ", _charDataService.SelectedLanguages.Select(l => l.Name));
    public bool HasLanguages => _charDataService.SelectedLanguages.Count > 0;
    public int CurrentHP => _charDataService.CurrentHitpoints;
    public int MaxHP => _charDataService.Hitpoints;
    // While transformed, Hit Protection means nothing: damage hits Stability, and the form drops before HP is touched.
    // The dashboard shows a Stability bar in its place.
    public bool ShowStability => IsTransformedNow;
    public bool ShowHitProtection => !IsTransformedNow;
    public int CurrentStability => _charDataService.Transform is { IsTransformed: true } t ? (int)t.StabilityCurrent : 0;
    public int MaxStability => _charDataService.Transform is { IsTransformed: true } t ? (int)t.StabilityMax : 0;
    public Color StabilityMaxColor => Colors.White;

    // ===== CURRENCY =====
    // Total in chips, then each denomination owned (highest first). Icons are colored to match each coin's description.
    public string CurrencyTotalText => CurrencyHelper.FormatAmount(_charDataService.CurrencyTotalChips, CurrencyDisplaySettings.Unit);
    public string CurrencyUnitIcon => CurrencyHelper.UnitIcon(CurrencyDisplaySettings.Unit);
    public Color CurrencyUnitColor => Color.FromArgb(CurrencyHelper.UnitColorHex(CurrencyDisplaySettings.Unit));
    public ObservableCollection<CurrencyDisplay> CurrencyBreakdown { get; } = new();

    public class CurrencyDisplay
    {
        public string Icon { get; set; } = string.Empty;
        public Color IconColor { get; set; } = Colors.White;
        public string CountText { get; set; } = string.Empty;
    }

    private static readonly (int Id, string Icon, string Color)[] CurrencyIcons =
    {
        (CurrencyHelper.NoteId,  "▬", "#4a90d9"),   // blue-backed paper
        (CurrencyHelper.MarkId,  "▮", "#e8e0c0"),   // stamped paper
        (CurrencyHelper.RupeeId, "▲", "#c0c0c0"),   // triangular silver
        (CurrencyHelper.ChipId,  "●", "#b87333"),   // round copper
    };

    private void UpdateCurrencyDisplay()
    {
        CurrencyBreakdown.Clear();
        foreach (var (id, icon, color) in CurrencyIcons)
        {
            int count = CurrencyHelper.Count(_charDataService.Inventory, id);
            if (count <= 0) continue;
            CurrencyBreakdown.Add(new CurrencyDisplay
            {
                Icon = icon,
                IconColor = Color.FromArgb(color),
                CountText = count.ToString("N0")
            });
        }
        OnPropertyChanged(nameof(CurrencyTotalText));
        OnPropertyChanged(nameof(CurrencyUnitIcon));
        OnPropertyChanged(nameof(CurrencyUnitColor));
    }

    // ===== STAT PROPERTIES =====
    public int StatVigor => _charDataService.TotalStatVigor;
    public int StatAgility => _charDataService.TotalStatAgility;
    public int StatMind => _charDataService.TotalStatMind;
    public int StatSpirit => _charDataService.TotalStatSpirit;

    public int CurrentVigor => _charDataService.CurrentStatVigor;
    public int CurrentAgility => _charDataService.CurrentStatAgility;
    public int CurrentMind => _charDataService.CurrentStatMind;
    public int CurrentSpirit => _charDataService.CurrentStatSpirit;
    public int ArmorRating => _charDataService.ArmorRating;
    public bool HasEquippedArmor => EquippedArmor != "None";

    // While transformed, the Vigor and Agility bars show the form's scores, marked with a wolf.
    private bool IsTransformedNow => _charDataService.Transform is { IsTransformed: true };
    public string VigorFormBadge => IsTransformedNow ? "🐺" : string.Empty;
    public string AgilityFormBadge => IsTransformedNow ? "🐺" : string.Empty;

    public string ArmorChevronsDisplay
    {
        get
        {
            var chevrons = new System.Text.StringBuilder();
            for (int i = 1; i <= 3; i++)
            {
                chevrons.Append(ArmorRating >= i ? "⬤" : "◯");
                if (i < 3) chevrons.Append(' ');
            }
            return chevrons.ToString();
        }
    }

    // ===== LIMB SET DISPLAY PROPERTIES =====
    public List<LimbSetDisplay> LimbSetDisplays { get; set; } = new();
    public List<BeltSlotDisplay> BeltSlotDisplays { get; set; } = new();

    public void UpdateBeltSlotDisplays()
    {
        BeltSlotDisplays.Clear();

        int slot = 1;
        while (slot <= 4)
        {
            var item = _charDataService.GetEquippedBeltSlot(slot);

            if (item != null && item.SlotIndex == slot && item.SlotsRequired > 1)
            {
                // This slot is where a bulky item's span begins — one merged cell.
                BeltSlotDisplays.Add(new BeltSlotDisplay
                {
                    SlotNumber = slot,
                    IsOccupied = true,
                    OccupiedByItemName = item.DisplayName,
                    OccupiedByItem = item,
                    IsPartOfBulkyItem = true,
                    SpanSlots = item.SlotsRequired
                });
                slot += item.SlotsRequired;
            }
            else if (item != null && item.SlotIndex != slot)
            {
                // Second half of a bulky item — already represented by the merged
                // cell above, so no separate cell is rendered here.
                slot++;
            }
            else
            {
                BeltSlotDisplays.Add(new BeltSlotDisplay
                {
                    SlotNumber = slot,
                    IsOccupied = item != null,
                    OccupiedByItemName = item?.DisplayName ?? string.Empty,
                    OccupiedByItem = item,
                    IsPartOfBulkyItem = false,
                    SpanSlots = 1
                });
                slot++;
            }
        }

        OnPropertyChanged(nameof(BeltSlotDisplays));
    }

    // ===== WIELDED WEAPONS (mini equipment dashboard) =====
    public ObservableCollection<WeaponDisplay> EquippedWeapons { get; } = new();
    public bool HasEquippedWeapons => EquippedWeapons.Count > 0;
    public bool HasNoEquippedWeapons => EquippedWeapons.Count == 0;

    public class WeaponDisplay
    {
        public int ItemId { get; set; }                  // 0 for a transformed form's natural weapon
        public bool CanRecordKill { get; set; }          // held in a hand (belted weapons can't make the killing blow)
        public string Name { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public List<InfoChip> SummaryChips { get; set; } = new();   // weapon type, damage type, die, speed
        public List<InfoChip> EffectChips { get; set; } = new();    // the weapon's effects and skills
        public bool HasEffects => EffectChips.Count > 0;
        // Damage information first, then the effects and skills, all on one row.
        public IEnumerable<InfoChip> AllChips => SummaryChips.Concat(EffectChips);
        public List<ChipLine> ChipLines => ChipLayout.Split(AllChips);
        public string Summary => string.Join(" ", SummaryChips.Select(c => c.Text));
        public string EffectsText => string.Join(", ", EffectChips.Select(c => c.Text));
    }

    private void UpdateEquippedWeapons()
    {
        EquippedWeapons.Clear();

        // Hands first, then belt, each in slot order
        var carried = _charDataService.Inventory
            .Where(i => i.IsEquipped && i.IsWeapon && i.Template != null
                        && (i.EquipmentSlot == EquipmentSlotType.Hand || i.EquipmentSlot == EquipmentSlotType.Belt))
            .OrderBy(i => i.EquipmentSlot == EquipmentSlotType.Hand ? 0 : 1)
            .ThenBy(i => i.SlotIndex ?? 0);

        foreach (var item in carried)
        {
            var t = item.Template!;

            var summary = new List<InfoChip>();
            if (t.WeaponKind is WeaponType.Ranged or WeaponType.Thrown)
                summary.Add(InfoChip.Plain(EnumDisplayHelper.GetName(t.WeaponKind.Value)));
            if (t.DamageType.HasValue) summary.Add(DamageTypeChip(t.DamageType.Value));
            if (t.WeaponDamage.HasValue) summary.Add(InfoChip.Plain(EnumDisplayHelper.GetName(t.WeaponDamage.Value)));
            if (t.WeaponSpeed.HasValue) summary.Add(InfoChip.Plain(EnumDisplayHelper.GetName(t.WeaponSpeed.Value)));

            var effects = (t.WeaponEffects ?? new List<WeaponEffectType>()).Select(e => EffectChip(t, e)).ToList();
            foreach (var chip in effects) chip.IsEffect = true;

            EquippedWeapons.Add(new WeaponDisplay
            {
                Name = item.DisplayName,
                ItemId = item.Id,
                CanRecordKill = item.EquipmentSlot == EquipmentSlotType.Hand && t.DamageType.HasValue,
                Location = item.EquipmentSlot == EquipmentSlotType.Hand ? $"Hand {item.SlotIndex}" : $"Belt {item.SlotIndex}",
                SummaryChips = summary,
                EffectChips = effects
            });
        }

        // Brawler: with a free hand, your fists are a weapon. One row, however many hands are free.
        if (_charDataService.HasFeature(BrawlerFeatureId) && HasFreeHand())
        {
            const string shockRule = "Your fists are legally categorized as fast weapons with a damage die of 1D4 bludgeoning and shock 4. " +
                                     "If the damage die becomes raised, shock increases to within the highest 2 digits of the die.";

            EquippedWeapons.Add(new WeaponDisplay
            {
                Name = "Fists",
                Location = "Brawler",
                SummaryChips = new List<InfoChip>
                {
                    InfoChip.Tappable("Bludgeoning", "Bludgeoning damage", shockRule),
                    InfoChip.Plain("D4"),
                    InfoChip.Plain("Fast")
                },
                EffectChips = new List<InfoChip>
                {
                    new InfoChip
                    {
                        Text = "Shock 4",
                        IsTappable = true,
                        IsEffect = true,
                        TapCommand = new Command(async () =>
                            await Application.Current!.MainPage!.DisplayAlertAsync("Shock 4 (Brawler)", shockRule, "OK"))
                    }
                }
            });
        }

        // While transformed, every weapon trait of the form is wielded too (named after its trait).
        if (_charDataService.Transform is { IsTransformed: true } form)
        {
            string where = string.IsNullOrWhiteSpace(form.Name) ? "Natural" : form.Name;
            foreach (var weapon in form.Weapons)
            {
                string effectsText = LiveShifter.WeaponEffectsText(weapon);
                EquippedWeapons.Add(new WeaponDisplay
                {
                    Name = weapon.Name,
                    Location = where,
                    SummaryChips = new List<InfoChip> { InfoChip.Plain(LiveShifter.WeaponSummary(weapon)) },
                    EffectChips = string.IsNullOrEmpty(effectsText)
                        ? new List<InfoChip>()
                        : new List<InfoChip> { new InfoChip { Text = effectsText, IsEffect = true } }
                });
            }
        }

        OnPropertyChanged(nameof(HasEquippedWeapons));
        OnPropertyChanged(nameof(HasNoEquippedWeapons));
    }

    private const int BrawlerFeatureId = 8;

    // A hand is free when it holds nothing. A two-handed weapon fills its whole set.
    private bool HasFreeHand()
    {
        foreach (var set in _charDataService.LimbSets)
        {
            if (set.IsOccupiedByTwoHandedItem) continue;
            foreach (int index in set.SlotIndices)
                if (_charDataService.GetEquippedHandSlot(index + 1) == null) return true;
        }
        return false;
    }

    // The damage type chip: tap for what it does and how far along the unlock is.
    private InfoChip DamageTypeChip(WeaponDamageType type)
    {
        int required = WeaponProficiencyTracker.KillsRequired;
        string name = EnumDisplayHelper.GetName(type);
        string rule = EnumDisplayHelper.GetDescription(type) ?? string.Empty;
        bool proficient = Proficiency.IsTypeProficient(type);

        string status = proficient
            ? "✅ You're proficient, so this effect applies to your attacks."
            : $"🔒 Locked. You have {Proficiency.TypeKills(type)} / {required} kills with {name} weapons. " +
                "It unlocks at 5 kills with that damage type, or with your starting choice.";

        return InfoChip.Tappable((proficient ? "" : "🔒 ") + name, $"{name} damage", $"{rule}\n\n{status}");
    }

    // An effect chip: tap for what it does, and for a weapon skill, whether you can use it yet.
    private InfoChip EffectChip(Item weapon, WeaponEffectType effect)
    {
        int required = WeaponProficiencyTracker.KillsRequired;
        string name = EnumDisplayHelper.GetName(effect);
        string description = EnumDisplayHelper.GetEffectDescription(effect);
        bool isSkill = WeaponProficiencyTracker.NeedsWeaponSkill(effect);
        bool usable = Proficiency.IsEffectUsable(weapon, effect);

        string status = !isSkill
            ? "This effect doesn't need weapon proficiency."
            : usable
                ? "✅ Unlocked. You have this weapon's skill proficiency."
                : $"🔒 Locked. You have {Proficiency.WeaponKills(weapon)} / {required} kills with this weapon. " +
                  "It unlocks at 5 kills with this specific weapon, or with a Warrior weapon pick.";

        return InfoChip.Tappable((usable ? "" : "🔒 ") + name, name, $"{description}\n\n{status}");
    }

    // ===== FAMILIAR STRIP (equipment column) =====
    public ObservableCollection<FamiliarStripItem> FamiliarStrip { get; } = new();
    public bool HasFamiliars => FamiliarStrip.Count > 0;

    public void RefreshFamiliarStrip()
    {
        FamiliarStrip.Clear();
        foreach (var familiar in _charDataService.AcquiredFamiliars)
            FamiliarStrip.Add(new FamiliarStripItem(_charDataService, familiar));
        OnPropertyChanged(nameof(HasFamiliars));
    }

    public class BeltSlotDisplay
    {
        public int SlotNumber { get; set; }
        public bool IsOccupied { get; set; }
        public string OccupiedByItemName { get; set; } = string.Empty;
        public CharacterItem? OccupiedByItem { get; set; }
        public bool IsPartOfBulkyItem { get; set; }
        public int SpanSlots { get; set; } = 1;
    }

    public void UpdateLimbSetDisplays()
    {
        LimbSetDisplays.Clear();

        foreach (var limbSet in _charDataService.LimbSets)
        {
            var display = new LimbSetDisplay
            {
                LimbSetId = limbSet.Id,
                DisplayName = limbSet.DisplayName,
                Icon = limbSet.Icon,
                PairType = limbSet.PairType,
                SlotCount = limbSet.SlotCount,
                IsOccupiedByTwoHandedItem = limbSet.IsOccupiedByTwoHandedItem,
                Slots = new List<LimbSlotDisplay>()
            };

            CharacterItem? twoHandedOccupant = limbSet.IsOccupiedByTwoHandedItem && limbSet.TwoHandedItemId.HasValue
                ? _charDataService.Inventory.FirstOrDefault(i => i.Id == limbSet.TwoHandedItemId.Value)
                : null;

            for (int i = 0; i < limbSet.SlotIndices.Count; i++)
            {
                int slotIndex = limbSet.SlotIndices[i];
                int slotNumber = slotIndex + 1;

                // A two-handed occupant shows on EVERY slot in its set, not just the
                // one matching its stored SlotIndex.
                var equippedItem = twoHandedOccupant ?? _charDataService.GetEquippedHandSlot(slotNumber);

                string slotName = limbSet.PairType == LimbPairType.Paired
                    ? (i == 0 ? limbSet.LeftSlotName : limbSet.RightSlotName)
                    : limbSet.DisplayName;

                var slotDisplay = new LimbSlotDisplay
                {
                    SlotNumber = slotNumber,
                    SlotName = slotName,
                    IsOccupied = equippedItem != null,
                    OccupiedByItemId = equippedItem?.Id ?? 0,
                    OccupiedByItemName = equippedItem?.DisplayName ?? string.Empty,
                    OccupiedByItem = equippedItem,
                    IsPartOfTwoHandedItem = limbSet.IsOccupiedByTwoHandedItem,
                    LimbSetId = limbSet.Id
                };

                display.Slots.Add(slotDisplay);
            }

            LimbSetDisplays.Add(display);
        }

        OnPropertyChanged(nameof(LimbSetDisplays));
    }

    public class LimbSetDisplay
    {
        public int LimbSetId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public string Icon { get; set; } = "🫳";
        public LimbPairType PairType { get; set; }
        public int SlotCount { get; set; }
        public bool IsOccupiedByTwoHandedItem { get; set; }
        public List<LimbSlotDisplay> Slots { get; set; } = new();
    }

    public class LimbSlotDisplay
    {
        public int SlotNumber { get; set; }
        public string SlotName { get; set; } = string.Empty;
        public bool IsOccupied { get; set; }
        public int OccupiedByItemId { get; set; }
        public string OccupiedByItemName { get; set; } = string.Empty;
        public CharacterItem? OccupiedByItem { get; set; }
        public bool IsPartOfTwoHandedItem { get; set; }
        public int LimbSetId { get; set; }
    }

    public string EquippedArmor
    {
        get => _equippedArmor;
        set { _equippedArmor = value; OnPropertyChanged(); }
    }

    public string EquippedHands
    {
        get => _equippedHands;
        set { _equippedHands = value; OnPropertyChanged(); }
    }

    public string EquippedBelt
    {
        get => _equippedBelt;
        set { _equippedBelt = value; OnPropertyChanged(); }
    }

    public int FatigueBarUsed => _charDataService.FatigueTotal;
    public int FatigueBarMax => _charDataService.FatigueBarMax;
    public int FatigueInInventory => _charDataService.FatigueInInventory;
    public bool HasFatigueOverflow => FatigueInInventory > 0;

    // ===== MODIFIER COLORS (red = reduced by an effect/fatigue/encumbrance, green = increased) =====
    private static readonly Color PenaltyColor = Color.FromArgb("#e74c3c");
    private static readonly Color BonusColor = Color.FromArgb("#2ecc71");

    private static Color ColorFor(int direction) =>
        direction < 0 ? PenaltyColor : direction > 0 ? BonusColor : Colors.White;

    public Color MaxHPColor => ColorFor(_charDataService.Direction(
        _charDataService.Hitpoints, () => _charDataService.BaseHitpoints));

    public Color VigorMaxColor => ColorFor(_charDataService.Direction(
        _charDataService.TotalStatVigor, () => _charDataService.TotalStatVigor));
    public Color AgilityMaxColor => ColorFor(_charDataService.Direction(
        _charDataService.TotalStatAgility, () => _charDataService.TotalStatAgility));
    public Color MindMaxColor => ColorFor(_charDataService.Direction(
        _charDataService.TotalStatMind, () => _charDataService.TotalStatMind));
    public Color SpiritMaxColor => ColorFor(_charDataService.Direction(
        _charDataService.TotalStatSpirit, () => _charDataService.TotalStatSpirit));

    public Color FatigueMaxColor => ColorFor(_charDataService.Direction(
        _charDataService.FatigueBarMax, () => _charDataService.FatigueBarMax));

    // Pack capacity after fatigue overflow, compared with the unmodified 6 + Vigor ASM
    public Color PackTotalColor => ColorFor(_charDataService.Direction(
        _charDataService.InventorySlotsAvailable, () => _charDataService.InventorySlotsTotal));

    public int InventorySlotsTotal => _charDataService.InventorySlotsAvailable;


    // ===== DROP MODE =====
    public bool IsDropModeActive
    {
        get => _isDropModeActive;
        set
        {
            _isDropModeActive = value;
            OnPropertyChanged();
            UpdateDropModeState();
        }
    }

    // ===== INVENTORY PROPERTIES =====
    public ObservableCollection<InventoryItemDisplay> InventoryItems
    {
        get => _inventoryItems;
        set
        {
            _inventoryItems = value;
            OnPropertyChanged();
        }
    }

    public InventoryItemDisplay? SelectedInventoryItem
    {
        get => _selectedInventoryItem;
        set
        {
            _selectedInventoryItem = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedItem));
            OnPropertyChanged(nameof(CanEquipSelectedToHand));
            OnPropertyChanged(nameof(CanAdjustSelectedUses));
            OnPropertyChanged(nameof(CanEquipSelectedToBelt));
            OnPropertyChanged(nameof(CanEquipSelectedAsArmor));
            OnPropertyChanged(nameof(IsSelectedEquippedInHand));
            OnPropertyChanged(nameof(IsSelectedEquippedOnBelt));
            OnPropertyChanged(nameof(IsSelectedEquippedAsArmor));
            OnPropertyChanged(nameof(SelectedItemName));
            OnPropertyChanged(nameof(SelectedItemDescription));
            OnPropertyChanged(nameof(SelectedItemDetails));
            OnPropertyChanged(nameof(SelectedItemPlayerNote));
            OnPropertyChanged(nameof(HasPlayerNote));
            OnPropertyChanged(nameof(HasUses));
            OnPropertyChanged(nameof(SelectedItemRemainingUses));
            OnPropertyChanged(nameof(SelectedItemIsEmpty));
            OnPropertyChanged(nameof(SelectedItemIsEquipped));
            RebuildSelectedItemDetails();
        }
    }

    public bool CanEquipSelectedToHand
    {
        get
        {
            var item = GetSelectedCharacterItem();
            return item != null
               && !(IsPackDropped && !item.IsEquipped)
               && EquipmentActionHelper.CanEquipToHand(item, _charDataService);
        }
    }

    public bool CanEquipSelectedToBelt
    {
        get
        {
            var item = GetSelectedCharacterItem();
            return item != null
               && !(IsPackDropped && !item.IsEquipped)
               && EquipmentActionHelper.CanEquipToBelt(item, _charDataService);
        }
    }

    // Uses can be changed on any item except one that is effectively dropped
    // (pack dropped and the item isn't equipped/belted).
    public bool CanAdjustSelectedUses
    {
        get
        {
            var item = GetSelectedCharacterItem();
            return item != null && !(IsPackDropped && !item.IsEquipped);
        }
    }

    private CharacterItem? GetSelectedCharacterItem()
    {
        if (SelectedInventoryItem == null) return null;
        return _charDataService.Inventory.FirstOrDefault(i => i.Id == SelectedInventoryItem.Id);
    }

    // ===== UNEQUIP / EQUIP AS ARMOR (item details panel) =====
    public bool CanEquipSelectedAsArmor
    {
        get
        {
            var item = GetSelectedCharacterItem();
            return item != null && EquipmentActionHelper.CanEquipAsArmor(item, _charDataService);
        }
    }

    // Unequip takes the place of whichever "Equip to ..." button matches the slot the item is in
    public bool IsSelectedEquippedInHand => IsSelectedEquippedIn(EquipmentSlotType.Hand);
    public bool IsSelectedEquippedOnBelt => IsSelectedEquippedIn(EquipmentSlotType.Belt);
    public bool IsSelectedEquippedAsArmor => IsSelectedEquippedIn(EquipmentSlotType.Armor);

    private bool IsSelectedEquippedIn(EquipmentSlotType slot)
    {
        var item = GetSelectedCharacterItem();
        return item != null && item.IsEquipped && item.EquipmentSlot == slot;
    }

    public ICommand UnequipSelectedCommand { get; }
    public ICommand EquipSelectedAsArmorCommand { get; }

    private async void OnUnequipSelected()
    {
        var item = GetSelectedCharacterItem();
        if (item == null) return;

        if (await EquipmentActionHelper.UnequipAsync(item, _charDataService))
        {
            await RefreshAfterSelectedItemChangeAsync(item.Id);
        }
    }

    private async void OnEquipSelectedAsArmor()
    {
        var item = GetSelectedCharacterItem();
        if (item == null) return;

        if (await EquipmentActionHelper.EquipAsArmorAsync(item, _charDataService))
        {
            await RefreshAfterSelectedItemChangeAsync(item.Id);
        }
    }

    // ===== EDIT WRITTEN VALUE (Notes) =====
    public bool CanEditSelectedValue
    {
        get
        {
            var item = GetSelectedCharacterItem();
            return item?.Template?.IsValueEditable == true && !(IsPackDropped && !item.IsEquipped);
        }
    }

    // Reloads the pack, then reselects the same record so the buttons show its new state.
    // A dropped item is gone, so nothing ends up selected.
    private async Task RefreshAfterSelectedItemChangeAsync(int itemId)
    {
        await LoadCharacterDataAsync();
        UpdateLimbSetDisplays();
        SelectedInventoryItem = InventoryItems.FirstOrDefault(i => i.Id == itemId);
    }

    public ICommand EquipSelectedToHandCommand { get; }
    public ICommand EquipSelectedToBeltCommand { get; }
    public ICommand HandToFamiliarCommand { get; }

    private async void OnEquipSelectedToHand()
    {
        var item = GetSelectedCharacterItem();
        if (item == null) return;

        if (await EquipmentActionHelper.EquipToHandAsync(item, _charDataService))
        {
            await RefreshAfterSelectedItemChangeAsync(item.Id);
        }
    }

    private async void OnEquipSelectedToBelt()
    {
        var item = GetSelectedCharacterItem();
        if (item == null) return;

        if (await EquipmentActionHelper.EquipToBeltAsync(item, _charDataService))
        {
            await RefreshAfterSelectedItemChangeAsync(item.Id);
        }
    }

    public bool CanHandSelectedToFamiliar =>
    FamiliarHandoffHelper.CanHand(GetSelectedCharacterItem(), _charDataService);

    private async void OnHandToFamiliar()
    {
        var item = GetSelectedCharacterItem();
        if (item == null) return;

        if (await FamiliarHandoffHelper.HandToFamiliarAsync(item, _charDataService))
        {
            SelectedInventoryItem = null;
            await LoadCharacterDataAsync();
        }
    }

    public bool HasSelectedItem => SelectedInventoryItem != null;
    public string SelectedItemName => SelectedInventoryItem?.DisplayName ?? string.Empty;
    public string SelectedItemDescription => SelectedInventoryItem?.Description ?? string.Empty;
    public string SelectedItemDetails => SelectedInventoryItem?.Details ?? string.Empty;
    public string SelectedItemPlayerNote => SelectedInventoryItem?.PlayerNote ?? string.Empty;
    public bool HasPlayerNote => !string.IsNullOrEmpty(SelectedItemPlayerNote);
    public bool HasUses => SelectedInventoryItem?.MaxUses > 0;
    public string SelectedItemRemainingUses => SelectedInventoryItem?.RemainingUses.ToString() ?? "0";
    public bool SelectedItemIsEmpty => SelectedInventoryItem?.IsEmpty ?? false;
    public bool SelectedItemIsEquipped => SelectedInventoryItem?.IsEquipped ?? false;

    // ===== ITEM DETAILS (structured rows in the details panel) =====
    public ObservableCollection<ItemDetailRow> SelectedItemDetailRows { get; } = new();
    public ObservableCollection<ItemEffectRow> SelectedItemEffectRows { get; } = new();
    public bool HasSelectedItemEffects => SelectedItemEffectRows.Count > 0;

    private void RebuildSelectedItemDetails()
    {
        SelectedItemDetailRows.Clear();
        SelectedItemEffectRows.Clear();

        var item = GetSelectedCharacterItem();
        var t = item?.Template;
        if (item != null && t != null)
        {
            AddDetail("Category", t.Category.HasValue ? EnumDisplayHelper.GetName(t.Category.Value) : null);
            AddDetail("Size", EnumDisplayHelper.GetName(t.Size));
            AddDetail("Value", t.IsValueEditable
                ? $"{CurrencyHelper.ValueEach(item):N0} chips (written)"
                : (t.ValueInChips.HasValue ? $"{t.ValueInChips} chips" : null));
            AddDetail("Stackable", t.IsStackableItem ? "Yes" : "No");
            if (t.IsStackableItem) AddDetail("Stack limit", t.QtyLimit?.ToString());
            if (t.HasUses) AddDetail("Uses", $"{item.RemainingUses} / {t.Uses}");
            if (item.HasUsageDie) AddDetail("Usage die", item.UsageDieLabel);
            if (t.IsWeapon) AddWeaponDetails(t);
            if (t.IsArmor)
            {
                AddDetail("Armor value", t.ArmorValue?.ToString());
                AddDetail("Armor type", t.ArmorType.HasValue ? EnumDisplayHelper.GetName(t.ArmorType.Value) : null);
            }
            if (t.IsContainer) AddDetail("Container slots", t.ContainerCapacity.ToString());
        }

        OnPropertyChanged(nameof(HasSelectedItemEffects));
    }

    private void AddDetail(string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) SelectedItemDetailRows.Add(new ItemDetailRow(label, value));
    }

    private void AddWeaponDetails(Item t)
    {
        AddDetail("Weapon type", t.WeaponKind.HasValue ? EnumDisplayHelper.GetName(t.WeaponKind.Value) : null);
        AddDetail("Damage type", t.DamageType.HasValue ? EnumDisplayHelper.GetName(t.DamageType.Value) : null);
        AddDetail("Damage", t.WeaponDamage.HasValue ? EnumDisplayHelper.GetName(t.WeaponDamage.Value) : null);
        AddDetail("Speed", t.WeaponSpeed.HasValue ? EnumDisplayHelper.GetName(t.WeaponSpeed.Value) : null);

        int required = WeaponProficiencyTracker.KillsRequired;
        var prof = Proficiency;

        // The damage type's rule, locked until the type is proficient.
        if (t.DamageType.HasValue)
        {
            var type = t.DamageType.Value;
            string typeName = EnumDisplayHelper.GetName(type);
            string rule = EnumDisplayHelper.GetDescription(type) ?? string.Empty;
            bool typeOk = prof.IsTypeProficient(type);

            AddDetail("Kills (" + typeName + ")", $"{prof.TypeKills(type)} / {required}");
            SelectedItemEffectRows.Add(new ItemEffectRow(
                (typeOk ? "" : "🔒 ") + typeName,
                typeOk ? rule : $"Locked until you have {required} kills with {typeName} weapons. When unlocked: {rule}"));
        }

        // Skill effects (Bleed, Shock, Sweep...) need kills with this specific weapon.
        bool weaponOk = prof.IsWeaponProficient(t);
        if (t.WeaponEffects is { Count: > 0 } && t.WeaponEffects.Any(WeaponProficiencyTracker.NeedsWeaponSkill))
            AddDetail("Kills (this weapon)", weaponOk && prof.WeaponKills(t) < required
                ? "Unlocked (Warrior)"
                : $"{prof.WeaponKills(t)} / {required}");

        foreach (var effect in t.WeaponEffects ?? new List<WeaponEffectType>())
        {
            string name = EnumDisplayHelper.GetName(effect);
            string description = EnumDisplayHelper.GetEffectDescription(effect);
            bool usable = prof.IsEffectUsable(t, effect);

            SelectedItemEffectRows.Add(new ItemEffectRow(
                (usable ? "" : "🔒 ") + name,
                usable ? description : $"Locked until you have {required} kills with this weapon. When unlocked: {description}"));
        }
    }

    public ICharAttribDataService CharDataService => _charDataService;

    // ===== FAVOUR (Shaman) =====
    public FavourTracker FavourTracker { get; }
    public ICommand FavourUpCommand { get; }
    public ICommand FavourDownCommand { get; }
    public int FavourAmount => _charDataService.Favour;
    public bool HasFavour => _charDataService.UsesFavour;

    // ===== SHIFTER =====
    private LiveShifter? _shifter;
    public LiveShifter Shifter => _shifter ??= LiveShifter.Create(_charDataService);

    // Raised after the form changes (the Features panel refreshes its status).
    public event Action? TransformationChanged;

    // The chip's (and the Features panel's) menu while transformed.
    public async Task ManageTransformationAsync()
    {
        var shifter = Shifter;
        var state = shifter.State;
        if (state == null) return;
        var page = Application.Current!.MainPage!;

        const string view = "📋 View my form";
        const string damage = "💥 Take damage (to Stability)";
        const string save = "🎲 Stability save";
        const string combat = "🏳️ Combat ended (restore Stability)";
        const string revert = "↩️ Revert to my normal form";

        string? choice = await page.DisplayActionSheetAsync(
            $"🐺 {(string.IsNullOrWhiteSpace(state.Name) ? "Transformed" : state.Name)} · Stability {state.StabilityCurrent}/{state.StabilityMax}",
            "Close", null, view, damage, save, combat, revert);
        if (choice == null || choice == "Close") return;

        if (choice == view)
        {
            await page.DisplayAlertAsync("Your Form", LiveShifter.FormSummary(state), "OK");
            return;
        }

        if (choice == damage)
        {
            string? answer = await page.DisplayPromptAsync(
                "Damage", "How much damage did you take?", "Apply", "Cancel", keyboard: Keyboard.Numeric);
            if (answer == null || !int.TryParse(answer.Trim(), out int amount) || amount <= 0) return;

            var outcome = await shifter.TakeDamageAsync(amount);
            await page.DisplayAlertAsync("Damage Applied", outcome.Message, "OK");
            if (outcome.NeedsSave) { await OpenStabilitySaveAsync(); return; }
        }
        else if (choice == save)
        {
            await OpenStabilitySaveAsync();
            return;
        }
        else if (choice == combat)
        {
            await shifter.RestoreStabilityAsync();
            await page.DisplayAlertAsync("Combat Ended", "Your Stability is back to its rolled value.", "OK");
        }
        else if (choice == revert)
        {
            bool ok = await page.DisplayAlertAsync("Revert", "Return to your normal form?", "Revert", "Cancel");
            if (!ok) return;
            await shifter.RevertAsync("by choice");
        }

        await LoadCharacterDataAsync();
        TransformationChanged?.Invoke();
    }

    private async Task OpenStabilitySaveAsync()
    {
        ModalOpening?.Invoke();   // keep an open panel in place
        var savePage = new StabilitySavePage(Shifter, async () =>
        {
            await LoadCharacterDataAsync();
            TransformationChanged?.Invoke();
        });
        await Application.Current!.MainPage!.Navigation.PushModalAsync(savePage);
    }

    // ===== COMMANDS =====
    public ICommand NavigateToFeaturesCommand { get; }
    public ICommand OpenOptionsCommand { get; }
    public ICommand InteractCommand { get; }
    public ICommand ExpendSelectedCommand { get; }
    public ICommand IncrementUsesCommand { get; }
    public ICommand DecrementUsesCommand { get; }
    public ICommand BestowCommand { get; }
    public ICommand ToggleDropModeCommand { get; }
    public ICommand EditNoteCommand { get; }
    public ICommand OpenBioCommand { get; }
    public ICommand OpenUpkeepCommand { get; }
    public ICommand OpenApplyDamageCommand { get; }
    public ICommand OpenApplyEffectCommand { get; }
    public ICommand RecoverItemCommand { get; }
    public ICommand OpenRollDieCommand { get; }
    public ICommand OpenEndOfCombatCommand { get; }

    // ===== INTERACT COMMAND =====
    private async void OnInteract()
    {
        if (SelectedInventoryItem == null) return;

        var characterItem = _charDataService.Inventory.FirstOrDefault(i => i.Id == SelectedInventoryItem.Id);
        if (characterItem == null) return;

        if (_charDataService.IsPackDropped && !characterItem.IsEquipped)
        {
            await Application.Current!.MainPage!.DisplayAlertAsync(
                "Pack Dropped",
                "You can't interact with items in your pack while it's dropped.",
                "OK");
            return;
        }

        var currentPage = Application.Current.MainPage;
        var homePage = currentPage as Views.Godrick_LiveGame.CharacterHomePage;

        if (homePage == null && currentPage?.Navigation?.ModalStack?.Count > 0)
        {
            homePage = currentPage.Navigation.ModalStack.OfType<Views.Godrick_LiveGame.CharacterHomePage>().FirstOrDefault();
        }

        if (homePage != null)
        {
            homePage.ShowFlyout(characterItem);
        }
        else
        {
            var flyout = new Views.Godrick_LiveGame.ItemFlyout(characterItem, _charDataService, async () =>
            {
                await LoadCharacterDataAsync();
            }, null);
            await Shell.Current.Navigation.PushModalAsync(flyout);
        }
    }

    // ===== EXPEND (destroyed for good: not recoverable, logged) =====
    private async void OnExpendSelected()
    {
        var item = GetSelectedCharacterItem();
        if (item == null) return;
        var page = Application.Current!.MainPage!;

        if (_charDataService.IsPackDropped && !item.IsEquipped)
        {
            await page.DisplayAlertAsync("Pack Dropped", "You can't expend items in your pack while it's dropped.", "OK");
            return;
        }
        if (item.IsEquipped)
        {
            await page.DisplayAlertAsync("Equipped", "Unequip this item before expending it.", "OK");
            return;
        }

        int quantity = 1;
        if (item.Quantity > 1)
        {
            string? text = await page.DisplayPromptAsync("Expend",
                $"How many {item.DisplayName} to expend? (1 to {item.Quantity})\nThis can't be undone.",
                "Expend", "Cancel", initialValue: "1", keyboard: Keyboard.Numeric);
            if (text == null) return;
            if (!int.TryParse(text, out quantity) || quantity < 1 || quantity > item.Quantity)
            {
                await page.DisplayAlertAsync("Invalid Amount", $"Enter a number from 1 to {item.Quantity}.", "OK");
                return;
            }
        }
        else if (!await page.DisplayAlertAsync("Expend Item",
                     $"Expend {item.DisplayName}? It will be gone for good and can't be recovered.", "Expend", "Cancel"))
        {
            return;
        }

        int id = item.Id;
        _charDataService.ExpendInventoryItem(id, quantity);

        await LoadCharacterDataAsync();
        SelectedInventoryItem = InventoryItems.FirstOrDefault(i => i.Id == id);   // null when the whole record is gone
    }

    // ===== EDIT ITEM (personal note, plus the written value on a Note) =====
    private async void OnEditNote()
    {
        var item = GetSelectedCharacterItem();
        if (item == null) return;

        bool canEditValue = item.Template?.IsValueEditable == true && !(IsPackDropped && !item.IsEquipped);

        var popup = new EditItemPopup(item, canEditValue);
        await Application.Current!.MainPage!.Navigation.PushModalAsync(popup);
        var result = await popup.Completion;
        if (result == null) return;

        _charDataService.EditItem(item.Id, result.PlayerNote, canEditValue ? result.WrittenValueInChips : null);

        // The tiles and panel read from the display objects, so push the saved note into them
        var display = InventoryItems.FirstOrDefault(i => i.Id == item.Id);
        if (display != null) display.PlayerNote = item.PlayerNote;
        if (SelectedInventoryItem != null && SelectedInventoryItem.Id == item.Id)
            SelectedInventoryItem.PlayerNote = item.PlayerNote;

        OnPropertyChanged(nameof(SelectedItemPlayerNote));
        OnPropertyChanged(nameof(HasPlayerNote));
        RebuildSelectedItemDetails();
        UpdateCurrencyDisplay();
    }

    // ===== USES ADJUSTMENT COMMANDS =====
    private void OnIncrementUses()
    {
        if (SelectedInventoryItem == null) return;
        var characterItem = _charDataService.Inventory.FirstOrDefault(i => i.Id == SelectedInventoryItem.Id);
        if (characterItem == null) return;
        if (_charDataService.IsPackDropped && !characterItem.IsEquipped) return;   // effectively dropped
        if (characterItem.Template?.Uses == null || characterItem.Template.Uses.Value <= 0) return;

        int maxUses = characterItem.Template.Uses.Value;
        if (characterItem.RemainingUses < maxUses)
        {
            int before = characterItem.RemainingUses;
            characterItem.RemainingUses++;
            characterItem.IsEmpty = characterItem.RemainingUses <= 0;
            characterItem.LastModified = DateTime.Now;
            _usesLog.Record(characterItem, before, characterItem.RemainingUses);
            RefreshSelectedItemDisplay();
        }
    }

    private async void OnDecrementUses()
    {
        if (SelectedInventoryItem == null) return;
        var characterItem = _charDataService.Inventory.FirstOrDefault(i => i.Id == SelectedInventoryItem.Id);
        if (characterItem == null) return;
        if (_charDataService.IsPackDropped && !characterItem.IsEquipped) return;   // effectively dropped
        if (characterItem.Template?.Uses == null || characterItem.Template.Uses.Value <= 0) return;
        if (characterItem.RemainingUses <= 0) return;

        int before = characterItem.RemainingUses;
        characterItem.RemainingUses--;
        characterItem.LastModified = DateTime.Now;

        if (characterItem.RemainingUses <= 0 && characterItem.Quantity > 1)
        {
            // A used-up item in a stack is consumed: quantity drops by one and the next one is fresh.
            characterItem.Quantity--;
            characterItem.RemainingUses = characterItem.Template.Uses.Value;
            characterItem.IsEmpty = false;
            _usesLog.Record(characterItem, before, 0, stackLeft: characterItem.Quantity);

            // Quantity affects pack slots, encumbrance and the tile, so rebuild, then reselect the same record.
            int selectedId = characterItem.Id;
            await LoadCharacterDataAsync();
            SelectedInventoryItem = InventoryItems.FirstOrDefault(i => i.Id == selectedId);
            return;
        }

        _usesLog.Record(characterItem, before, characterItem.RemainingUses);

        characterItem.IsEmpty = characterItem.RemainingUses <= 0;   // a single item stays "Empty" until dropped
        RefreshSelectedItemDisplay();
    }

    private void RefreshSelectedItemDisplay()
    {
        if (SelectedInventoryItem == null) return;
        var characterItem = _charDataService.Inventory.FirstOrDefault(i => i.Id == SelectedInventoryItem.Id);
        if (characterItem == null) return;

        SelectedInventoryItem.RemainingUses = characterItem.RemainingUses;
        SelectedInventoryItem.IsEmpty = characterItem.IsEmpty;
        SelectedInventoryItem.Details = GetItemDetails(characterItem);

        OnPropertyChanged(nameof(SelectedItemRemainingUses));
        OnPropertyChanged(nameof(SelectedItemIsEmpty));
        OnPropertyChanged(nameof(SelectedItemDetails));
        OnPropertyChanged(nameof(HasUses));
        RebuildSelectedItemDetails();
    }

    // ===== BESTOW COMMAND =====
    private async void OnBestow()
    {
        var before = InventoryTotals();   // what the pack held, so the toast can say what arrived
        var bestowPage = new BestowItemPage(_itemDataService, _charDataService, _actionLogService, async () =>
        {
            await LoadCharacterDataAsync();
            AnnounceBestowed(before);
        });
        await Shell.Current.Navigation.PushModalAsync(bestowPage);
    }

    // ===== DROP MODE =====
    private void OnToggleDropMode()
    {
        if (_charDataService.IsPackDropped) return;   // Drop Mode is unavailable while the pack is dropped
        IsDropModeActive = !IsDropModeActive;
    }

    private void UpdateDropModeState()
    {
        foreach (var item in InventoryItems)
        {
            item.IsDropMode = IsDropModeActive;
        }
        OnPropertyChanged(nameof(InventoryItems));
    }

    // ===== LOAD CHARACTER DATA =====
    public async Task LoadCharacterDataAsync()
    {
        var perfWatch = System.Diagnostics.Stopwatch.StartNew();
        _charDataService.RepairLimbSetState();
        _charDataService.UpdateEncumbranceState();
        InventoryItems.Clear();

        foreach (var characterItem in _charDataService.Inventory)
        {
            if (characterItem.IsDropped) continue;

            var template = characterItem.Template;
            var effectiveSize = template == null
                ? ItemSize.Regular
                : InventorySlotCalculator.EffectiveSize(template, characterItem.Quantity);

            InventoryItems.Add(new InventoryItemDisplay
            {
                Id = characterItem.Id,
                DisplayName = characterItem.DisplayName,
                Description = characterItem.DisplayDescription,
                Details = GetItemDetails(characterItem),
                Quantity = characterItem.Quantity,
                IsEmpty = characterItem.IsEmpty,
                IsEquipped = characterItem.IsEquipped,
                EquippedSlot = characterItem.SlotIndex,
                PlayerNote = characterItem.PlayerNote,
                MaxUses = template?.Uses ?? 0,
                RemainingUses = characterItem.RemainingUses,
                UsageDieSides = characterItem.HasUsageDie ? characterItem.UsageDieSides : 0,
                Rarity = template?.Rarity,
                IsDropMode = IsDropModeActive,
                IsStackable = template?.IsStackableItem ?? false,
                EquippedSlotType = GetEquipmentSlotType(characterItem),
                SizeLabel = effectiveSize switch
                {
                    ItemSize.Petty => "Petty",
                    ItemSize.Bulky => "Bulky",
                    _ => string.Empty
                }
            });
        }

        ActiveEffects.Clear();
        foreach (var effect in _charDataService.ActiveEffects)
        {
            ActiveEffects.Add(effect);
        }
        UpdateEquipmentDisplay();
        await Shifter.RemoveStrayFormPackAsync();
        RefreshFamiliarStrip();
        UpdateCurrencyDisplay();
        OnPropertyChanged(nameof(InventorySlotsUsed));
        OnPropertyChanged(nameof(InventorySlotsTotal));
        OnPropertyChanged(nameof(IsEncumbered));
        OnPropertyChanged(nameof(IsPackDropped));
        OnPropertyChanged(nameof(CanAdjustSelectedUses));
        OnPropertyChanged(nameof(PackToggleText));
        OnPropertyChanged(nameof(CanToggleDropMode));
        OnPropertyChanged(nameof(CanEquipSelectedToHand));
        OnPropertyChanged(nameof(CanEquipSelectedToBelt));

        // Names must refresh too: this singleton ViewModel is reused when a different character loads
        OnPropertyChanged(nameof(CharacterName));
        OnPropertyChanged(nameof(PlayerName));
        OnPropertyChanged(nameof(FavourAmount));
        OnPropertyChanged(nameof(HasFavour));

        // Force refresh stat bindings
        OnPropertyChanged(nameof(StatVigor));
        OnPropertyChanged(nameof(StatAgility));
        OnPropertyChanged(nameof(StatMind));
        OnPropertyChanged(nameof(StatSpirit));
        OnPropertyChanged(nameof(CurrentHP));
        OnPropertyChanged(nameof(MaxHP));
        OnPropertyChanged(nameof(ShowStability));
        OnPropertyChanged(nameof(ShowHitProtection));
        OnPropertyChanged(nameof(CurrentStability));
        OnPropertyChanged(nameof(MaxStability));
        OnPropertyChanged(nameof(CurrentVigor));
        OnPropertyChanged(nameof(CurrentAgility));
        OnPropertyChanged(nameof(CurrentMind));
        OnPropertyChanged(nameof(CurrentSpirit));
        OnPropertyChanged(nameof(VigorFormBadge));
        OnPropertyChanged(nameof(AgilityFormBadge));
        OnPropertyChanged(nameof(FatigueBarUsed));
        OnPropertyChanged(nameof(FatigueBarMax));
        OnPropertyChanged(nameof(FatigueInInventory));
        OnPropertyChanged(nameof(HasFatigueOverflow));
        OnPropertyChanged(nameof(NeedsMet));
        OnPropertyChanged(nameof(IsRested));
        OnPropertyChanged(nameof(IsDeprived));
        OnPropertyChanged(nameof(HasBioAlert));
        OnPropertyChanged(nameof(HasIncomingDamage));
        OnPropertyChanged(nameof(HasIncomingEffect));
        OnPropertyChanged(nameof(ApplyDamageButtonText));
        OnPropertyChanged(nameof(ApplyEffectButtonText));
        OnPropertyChanged(nameof(ArmorRating));
        OnPropertyChanged(nameof(ArmorChevronsDisplay));
        OnPropertyChanged(nameof(MaxHPColor));
        OnPropertyChanged(nameof(VigorMaxColor));
        OnPropertyChanged(nameof(AgilityMaxColor));
        OnPropertyChanged(nameof(MindMaxColor));
        OnPropertyChanged(nameof(SpiritMaxColor));
        OnPropertyChanged(nameof(FatigueMaxColor));
        OnPropertyChanged(nameof(PackTotalColor));
        OnPropertyChanged(nameof(CharacterRace));
        OnPropertyChanged(nameof(RaceFeatureName));
        OnPropertyChanged(nameof(RaceFeatureDescription));
        OnPropertyChanged(nameof(HasRaceInfo));
        OnPropertyChanged(nameof(HasRaceFeature));
        OnPropertyChanged(nameof(HasRaceFeatureDescription));
        OnPropertyChanged(nameof(LanguagesText));
        OnPropertyChanged(nameof(HasLanguages));
        OnPropertyChanged(nameof(CanEquipSelectedAsArmor));
        OnPropertyChanged(nameof(IsSelectedEquippedInHand));
        OnPropertyChanged(nameof(IsSelectedEquippedOnBelt));
        OnPropertyChanged(nameof(IsSelectedEquippedAsArmor));
        if (!IsViewer) _ = PromptStartingProficiencyAsync();   // the player's choice, never the GM's

        System.Diagnostics.Debug.WriteLine($"[PERF] LoadCharacterDataAsync: {perfWatch.ElapsedMilliseconds} ms");
        if (SelectedInventoryItem != null)
        {
            RefreshSelectedItemDisplay();
        }
    }

    private string GetEquipmentSlotType(CharacterItem item)
    {
        if (!item.IsEquipped) return string.Empty;

        return item.EquipmentSlot switch
        {
            EquipmentSlotType.Armor => "Armor",
            EquipmentSlotType.Belt => "Belt",
            EquipmentSlotType.Hand => "Hand",
            _ => string.Empty
        };
    }

    // ===== GET ITEM DETAILS =====
    private string GetItemDetails(CharacterItem item)
    {
        var details = new List<string>();

        if (item.Template?.ValueInChips != null)
            details.Add($"Value: {item.Template.ValueInChips} chips");

        if (item.Template?.WeaponDamage != null)
            details.Add($"Damage: {item.Template.WeaponDamage}");

        if (item.Template?.ArmorValue != null)
            details.Add($"Armor: +{item.Template.ArmorValue}");

        if (item.Template?.Uses != null && item.Template.Uses.Value > 0)
            details.Add($"Uses: {item.RemainingUses}/{item.Template.Uses}");

        if (item.HasUsageDie)
            details.Add($"Usage Die: {item.UsageDieLabel}");

        return details.Count > 0 ? string.Join(" | ", details) : "No additional details";
    }

    // ===== UPDATE EQUIPMENT DISPLAY =====
    private void UpdateEquipmentDisplay()
    {
        // Update armor. While transformed, the armoring trait's name stands in for the armor's name.
        var armor = _charDataService.GetEquippedArmor();
        var form = _charDataService.Transform is { IsTransformed: true } t ? t : null;
        EquippedArmor = !string.IsNullOrEmpty(form?.ArmorName)
            ? form!.ArmorName!
            : armor?.DisplayName ?? "None";
        OnPropertyChanged(nameof(HasEquippedArmor));

        // Update equipped counts
        int totalSlots = _charDataService.GetTotalLimbSlots();
        int usedHandSlots = 0;
        for (int i = 1; i <= totalSlots; i++)
        {
            var item = _charDataService.GetEquippedHandSlot(i);
            if (item != null) usedHandSlots += 1;
        }
        EquippedHands = $"{usedHandSlots} / {totalSlots}";

        int usedBeltSlots = 0;
        for (int i = 1; i <= 4; i++)
        {
            var item = _charDataService.GetEquippedBeltSlot(i);
            if (item != null) usedBeltSlots += 1;
        }
        EquippedBelt = $"{usedBeltSlots} / 4";

        OnPropertyChanged(nameof(ArmorRating));
        OnPropertyChanged(nameof(ArmorChevronsDisplay));
        UpdateBeltSlotDisplays();
        UpdateEquippedWeapons();
    }

    // ===== OPTIONS (Preferences / Quit) =====
    private async Task OnOpenOptions()
    {
        if (CloseViewerAction != null)   // in the GM's viewer this button is Close Viewer
        {
            CloseViewerAction();
            return;
        }
        const string preferences = "⚙️ Preferences";
        string quit = SessionMode.GmHosted ? "🚪 Leave Session" : "🚪 Quit";

        string? choice = await Application.Current!.MainPage!.DisplayActionSheetAsync(
            "Options", "Cancel", null, preferences, quit);

        if (choice == preferences)
        {
            var page = new PreferencesPage(
                IPlatformApplication.Current!.Services.GetRequiredService<SettingsViewModel>());
            await Application.Current!.MainPage!.Navigation.PushModalAsync(page);
        }
        else if (choice == quit)
        {
            await OnQuit();   // keeps the "Leave Session?" warning and ends the session
        }
    }

    // ===== QUIT COMMAND =====
    private async Task OnQuit()
    {
        bool inSession = SessionMode.GmHosted;

        bool confirm = await Application.Current.MainPage.DisplayAlertAsync(
            "Leave Session?",
            inSession
                ? "Your character will be saved and you'll leave the GM's session. You can rejoin while it is open. Any items you've dropped will NOT be recoverable after you leave."
                : "Your character will be saved and you'll return to the main menu. Any items you've dropped will NOT be recoverable after you leave.",
            "Yes, Leave",
            "Cancel");

        if (!confirm) return;

        string fileName = await _persistenceService.GenerateFileName(
            _charDataService.PlayerName,
            _charDataService.CharacterName);

        var saveData = _charDataService.CreateSaveData();
        _charDataService.FileName = fileName;
        saveData.FileName = fileName;
        saveData.LastSaved = DateTime.Now;
        saveData.CurrentPage = "CharacterHomePage";

        await _persistenceService.SaveCharacterDataAsync(saveData);
        _charDataService.MarkCharacterSaved();

        await _usesLog.FlushAllAsync();   // write any pending uses entries before the session ends
        await FavourTracker.FlushAsync();

        // In a GM's session: the GM gets a copy of the character as it was just saved, then we leave properly.
        if (inSession)
        {
            var session = IPlatformApplication.Current!.Services.GetRequiredService<IPlayerSession>();

            if (!await session.SendFinalCopyAsync())
            {
                bool leaveAnyway = await Application.Current.MainPage.DisplayAlertAsync(
                    "GM Copy Not Confirmed",
                    "Your character is saved on this device, but the GM couldn't confirm receiving a copy. " +
                    "If you leave now, the GM keeps the copy from the last time one was received. Leave anyway?",
                    "Leave Anyway",
                    "Stay");
                if (!leaveAnyway) return;
            }

            await session.LeaveAsync();
        }

        await _charDataService.EndSessionAsync();   // ends the session: dropped items are gone for good

        await Shell.Current.GoToAsync("///MainPage");
    }

    private async Task OnOpenUpkeep()
    {
        ModalOpening?.Invoke();   // keep an open panel in place
        var page = new UpkeepPage(_charDataService, PassDayHandler, async () => await LoadCharacterDataAsync());
        await Application.Current!.MainPage!.Navigation.PushModalAsync(page);
    }

    private async Task OnOpenBio()
    {
        ModalOpening?.Invoke();   // keep an open panel in place
        var popup = new BioPage(_charDataService, Proficiency, _itemDataService, _actionLogService, async () => await LoadCharacterDataAsync());
        await Application.Current!.MainPage!.Navigation.PushModalAsync(popup);
    }

    // The Bio button flashes for unspent training points, and (GM-hosted only) for a level the GM granted.
    public bool HasBioAlert => _charDataService.UnspentTrainingPoints > 0
                               || (SessionMode.GmHosted && _charDataService.PendingLevels > 0);

    // ===== SENT BY THE GM =====
    // Damage and effects the GM sent wait on the character until the player takes them on.
    // The Apply Damage / Apply Effect buttons flash while anything is waiting.
    public bool HasIncomingDamage => _charDataService.PendingDamage.Count > 0;
    public bool HasIncomingEffect => _charDataService.PendingEffects.Count > 0;

    public string ApplyDamageButtonText => _charDataService.PendingDamage.Count > 1
        ? $"⚔️ Apply Damage ({_charDataService.PendingDamage.Count})"
        : "⚔️ Apply Damage";

    public string ApplyEffectButtonText => _charDataService.PendingEffects.Count > 1
        ? $"✨ Apply Effect ({_charDataService.PendingEffects.Count})"
        : "✨ Apply Effect";

    // The GM's damage or effect just arrived over the network: make the Apply buttons notice it and start flashing.
    public void RefreshIncoming() => MainThread.BeginInvokeOnMainThread(() =>
    {
        OnPropertyChanged(nameof(HasIncomingDamage));
        OnPropertyChanged(nameof(HasIncomingEffect));
        OnPropertyChanged(nameof(ApplyDamageButtonText));
        OnPropertyChanged(nameof(ApplyEffectButtonText));
    });

    // ===== GM VIEWER =====
    // Set when the GM opens this page to look at a player's character. The page then works on a read-only
    // copy, and the Options button becomes Close Viewer.
    public Action? CloseViewerAction { get; set; }
    public bool IsViewer => CloseViewerAction != null;
    public bool IsLiveCharacter => CloseViewerAction == null;
    public string OptionsButtonText => IsViewer ? "✖ Close Viewer" : "⚙️ Options";
    public IPlayerActionLogService ActionLog => _actionLogService;

    // The page is going away: stop listening to anything that outlives it.
    public void Detach() => CurrencyDisplaySettings.Changed -= UpdateCurrencyDisplay;

    // Raised just before a dashboard page opens, so the home page can keep an open panel (Crafting, Shop...) in place.
    public event Action? ModalOpening;

    private async Task OnOpenRollDie()
    {
        ModalOpening?.Invoke();
        var popup = new DiceRollPopup(_charDataService);
        await Application.Current!.MainPage!.Navigation.PushModalAsync(popup);
    }

    private async Task OnOpenEndOfCombat()
    {
        ModalOpening?.Invoke();
        var popup = new EndOfCombatPopup(_charDataService, () => _ = LoadCharacterDataAsync());
        await Application.Current!.MainPage!.Navigation.PushModalAsync(popup);
    }

    private async Task OnOpenApplyDamage()
    {
        // Damage the GM sent comes first: its details are locked and the player chooses how to respond.
        var incoming = _charDataService.PendingDamage.FirstOrDefault();
        var popup = new ApplyDamagePopup(_charDataService, async () => await LoadCharacterDataAsync(), incoming);
        await Application.Current!.MainPage!.Navigation.PushModalAsync(popup);
    }

    private async Task OnOpenApplyEffect()
    {
        // An effect the GM sent comes first, locked: the player takes it on or ignores it.
        var incoming = _charDataService.PendingEffects.FirstOrDefault();
        var popup = incoming != null
            ? new ApplyEffectPopup(_charDataService, async () => await LoadCharacterDataAsync(), incoming)
            : new ApplyEffectPopup(_charDataService, async () => await LoadCharacterDataAsync());
        await Application.Current!.MainPage!.Navigation.PushModalAsync(popup);
    }

    private async void OnRemoveEffect(CharacterEffect? effect)
    {
        if (effect == null) return;

        if (effect.HardcodedType == HardcodedEffectType.Transformed)
        {
            await ManageTransformationAsync();
            return;
        }

        if (effect.HardcodedType == HardcodedEffectType.VigorSave)
        {
            var popup = new VigorSavePopup(_charDataService, async () => await LoadCharacterDataAsync());
            await Application.Current!.MainPage!.Navigation.PushModalAsync(popup);
            return;
        }

        if (effect.HardcodedType == HardcodedEffectType.DeathsDoor)
        {
            bool confirmClear = await Application.Current!.MainPage!.DisplayAlertAsync(
                "Clear Death's Door?", "Only clear this once the situation has been resolved.", "Yes", "Cancel");
            if (!confirmClear) return;

            _charDataService.ClearDeathsDoor();
            await LoadCharacterDataAsync();
            return;
        }

        var detailPopup = new EffectDetailPopup(effect, _charDataService, async () => await LoadCharacterDataAsync());
        await Application.Current!.MainPage!.Navigation.PushModalAsync(detailPopup);
    }

    // ===== INVENTORY CAPACITY =====
    public int InventorySlotsUsed => _charDataService.InventorySlotsUsed;
    public bool IsEncumbered => _charDataService.IsEncumbered;

    // ===== DROP PACK =====
    public bool IsPackDropped => _charDataService.IsPackDropped;
    public string PackToggleText => IsPackDropped ? "🎒 Pick Up Pack" : "🎒 Drop Pack";
    public bool CanToggleDropMode => !IsPackDropped;
    public ICommand TogglePackCommand { get; }

    private async Task OnTogglePack()
    {
        if (_charDataService.IsPackDropped)
        {
            // Picking the pack up can put you straight back into encumbrance
            bool wouldEncumber = _charDataService.InventorySlotsUsed > _charDataService.InventorySlotsAvailable;
            if (wouldEncumber)
            {
                bool confirm = await Application.Current!.MainPage!.DisplayAlertAsync(
                    "Pick Up Pack?",
                    "Your pack is over capacity. Picking it up will make you encumbered and reduce your Max HP to 0.",
                    "Pick Up",
                    "Cancel");
                if (!confirm) return;
            }
            _charDataService.SetPackDropped(false);
        }
        else
        {
            _charDataService.SetPackDropped(true);
        }
        if (_charDataService.IsPackDropped && IsDropModeActive)
        {
            IsDropModeActive = false;   // dropping the pack ends Drop Mode
        }
        await LoadCharacterDataAsync();
    }

    public ICommand ExplainEncumbranceCommand { get; }

    private async Task OnExplainEncumbrance()
    {
        if (!IsEncumbered) return;

        // Pack capacity follows the character's normal Vigor, even while transformed.
        int vigor = _charDataService.PackVigorModifier;
        int fatigue = _charDataService.FatigueInInventory;
        int total = _charDataService.InventorySlotsTotal;
        int available = _charDataService.InventorySlotsAvailable;
        int used = _charDataService.InventorySlotsUsed;

        int packMule = total - 6 - vigor;
        string capacity = $"Your pack holds 6 slots plus your Vigor modifier ({(vigor >= 0 ? "+" : "")}{vigor})" +
                          (packMule > 0 ? $" plus {packMule} from Pack Mule" : "") + $" = {total}";
        if (fatigue > 0) capacity += $", minus {fatigue} taken up by fatigue overflow = {available}";
        capacity += ".";

        string message =
            $"You're carrying {used} pack slots, but your pack only holds {available}.\n\n" +
            $"{capacity}\n\n" +
            "While you're encumbered your Max HP is 0.\n\n" +
            "To fix it: drop items, hand some to a familiar, clear fatigue, or move items to your hands or belt " +
            "(equipped items don't use pack slots).";

        await Application.Current!.MainPage!.DisplayAlertAsync("Encumbered", message, "OK");
    }

    // ===== DAILY NEEDS / DEPRIVATION =====
    public bool NeedsMet => _charDataService.NeedsMet;
    public bool IsRested => _charDataService.Rested;
    public bool IsDeprived => _charDataService.IsDeprived;

    public ICommand ExplainDeprivationCommand { get; }

    // Set by the home page: runs LiveFeatures.PassDayAsync and refreshes the Features panel.
    public Func<Task<FeatureUseResult>>? PassDayHandler { get; set; }

    // ===== INotifyPropertyChanged =====
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // Writes pending uses and Favour entries now (the Event Tray calls this when it opens).
    public async Task FlushPendingLogAsync()
    {
        await _usesLog.FlushAllAsync();
        await FavourTracker.FlushAsync();
    }

}