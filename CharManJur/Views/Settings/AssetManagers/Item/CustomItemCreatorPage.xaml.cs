using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;
using CharManJur.Helpers;

namespace CharManJur.Views;

public partial class CustomItemCreatorPage : ContentPage
{
    private readonly CustomItemCreatorViewModel _viewModel;

    public CustomItemCreatorPage(CustomItemCreatorViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // Populate pickers
        PopulatePickers();
        UpdateUsageDieVisibility();

        // Set default category selection to trigger visibility
        pickerCategory.SelectedIndexChanged += (s, e) =>
        {
            UpdateContainerVisibility();
            UpdateCategoryHelp();
        };
        pickerCategory.SelectedIndex = 0;

        if (_viewModel.IsEditMode || _viewModel.IsCopyMode)
        {
            PopulateFormForEdit();
        }
        else
        {
            // New items start at rarity 0 and Regular size. Applied once the page is on screen;
            // setting these pickers any earlier left them blank and unresponsive.
            Loaded += OnFirstLoaded;
        }
    }

    private void OnFirstLoaded(object? sender, EventArgs e)
    {
        Loaded -= OnFirstLoaded;
        pickerRarity.SelectedIndex = 0;   // 0 - None
        pickerSize.SelectedIndex = _viewModel.Sizes.IndexOf(nameof(ItemSize.Regular));
    }

    private void PopulatePickers()
    {
        // Categories
        pickerCategory.ItemsSource = _viewModel.Categories;

        // Sizes
        pickerSize.ItemsSource = _viewModel.Sizes;

        // Rarity
        pickerRarity.ItemsSource = _viewModel.RarityLevels;

        // Weapon
        pickerWeaponKind.ItemsSource = _viewModel.WeaponKinds;
        pickerDamageType.ItemsSource = _viewModel.DamageTypes;
        pickerWeaponKind.SelectedIndexChanged += OnWeaponKindChanged;
        pickerWeaponDamage.ItemsSource = _viewModel.WeaponDamageDice;
        pickerWeaponSpeed.ItemsSource = _viewModel.WeaponSpeeds;

        // Armor
        pickerArmorType.ItemsSource = _viewModel.ArmorTypes;

        // Weapon Effects
        cvWeaponEffects.ItemsSource = _viewModel.WeaponEffects;

        // Usage die
        pickerUsageDie.ItemsSource = _viewModel.UsageDieLabels;
        pickerUsageDie.SelectedIndex = _viewModel.UsageDieLabels.IndexOf(UsageDie.Label(_viewModel.UsageDieSides));
        chkUsageDie.CheckedChanged += (_, _) => UpdateUsageDieVisibility();
        chkStackable.CheckedChanged += (_, _) => UpdateUsageDieVisibility();
    }

    private void PopulateFormForEdit()
    {
        entryName.Text = _viewModel.Name;
        editorDescription.Text = _viewModel.Description;

        if (_viewModel.ValueInChips.HasValue) entryValue.Text = _viewModel.ValueInChips.Value.ToString();
        if (_viewModel.QtyLimit.HasValue) entryQtyLimit.Text = _viewModel.QtyLimit.Value.ToString();
        if (_viewModel.Uses.HasValue) entryUses.Text = _viewModel.Uses.Value.ToString();
        if (_viewModel.ArmorValue.HasValue) entryArmorValue.Text = _viewModel.ArmorValue.Value.ToString();

        chkStackable.IsChecked = _viewModel.IsStackable;

        if (_viewModel.Category.HasValue)
            pickerCategory.SelectedIndex = _viewModel.Categories.IndexOf(_viewModel.Category.Value.ToString());

        pickerSize.SelectedIndex = _viewModel.Sizes.IndexOf(_viewModel.Size.ToString());

        if (_viewModel.Rarity.HasValue)
            pickerRarity.SelectedIndex = Math.Clamp(_viewModel.Rarity ?? 0, 0, 3);

        if (_viewModel.WeaponKind.HasValue)
            pickerWeaponKind.SelectedIndex = _viewModel.WeaponKinds.IndexOf(_viewModel.WeaponKind.Value.ToString());

        if (_viewModel.DamageType.HasValue)
            pickerDamageType.SelectedIndex = _viewModel.DamageTypes.IndexOf(_viewModel.DamageType.Value.ToString());

        if (_viewModel.WeaponDamage.HasValue)
            pickerWeaponDamage.SelectedIndex = _viewModel.WeaponDamageDice.IndexOf(_viewModel.WeaponDamage.Value.ToString());

        if (_viewModel.WeaponSpeed.HasValue)
            pickerWeaponSpeed.SelectedIndex = _viewModel.WeaponSpeeds.IndexOf(_viewModel.WeaponSpeed.Value.ToString());

        if (_viewModel.ArmorType.HasValue)
            pickerArmorType.SelectedIndex = _viewModel.ArmorTypes.IndexOf(_viewModel.ArmorType.Value.ToString());

        if (_viewModel.ContainerSlots.HasValue) entryContainerSlots.Text = _viewModel.ContainerSlots.Value.ToString();
        chkUsageDie.IsChecked = _viewModel.HasUsageDie;
        chkHybrid.IsChecked = _viewModel.HybridUsage;
        pickerUsageDie.SelectedIndex = _viewModel.UsageDieLabels.IndexOf(UsageDie.Label(_viewModel.UsageDieSides));
        UpdateUsageDieVisibility();
        UpdateContainerVisibility();
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        try
        {
            // === READ ALL FIELDS FROM UI CONTROLS ===
            _viewModel.Name = entryName.Text?.Trim();
            _viewModel.Description = editorDescription.Text?.Trim();

            // Value
            if (!string.IsNullOrWhiteSpace(entryValue.Text))
            {
                _viewModel.ValueInChips = int.TryParse(entryValue.Text, out int val) ? val : null;
            }
            else
            {
                _viewModel.ValueInChips = null;
            }

            // QtyLimit
            if (!string.IsNullOrWhiteSpace(entryQtyLimit.Text))
            {
                _viewModel.QtyLimit = int.TryParse(entryQtyLimit.Text, out int val) ? val : null;
            }
            else
            {
                _viewModel.QtyLimit = null;
            }

            // Uses
            if (!string.IsNullOrWhiteSpace(entryUses.Text))
            {
                _viewModel.Uses = int.TryParse(entryUses.Text, out int val) ? val : null;
            }
            else
            {
                _viewModel.Uses = null;
            }

            // Armor Value
            if (!string.IsNullOrWhiteSpace(entryArmorValue.Text))
            {
                _viewModel.ArmorValue = int.TryParse(entryArmorValue.Text, out int val) ? val : null;
            }
            else
            {
                _viewModel.ArmorValue = null;
            }

            // Stackable
            _viewModel.IsStackable = chkStackable.IsChecked;

            // Usage die: never stackable (each one carries its own die)
            _viewModel.HasUsageDie = chkUsageDie.IsChecked;
            _viewModel.HybridUsage = chkUsageDie.IsChecked && chkHybrid.IsChecked;
            if (_viewModel.HasUsageDie)
            {
                if (pickerUsageDie.SelectedIndex < 0)
                {
                    await DisplayAlertAsync("Usage Die", "Choose the starting die for this item.", "OK");
                    return;
                }
                if (_viewModel.HybridUsage && (_viewModel.Uses ?? 0) < 1)
                {
                    await DisplayAlertAsync("Hybrid Usage", "Hybrid usage needs a Uses value of 1 or more, as well as the die.", "OK");
                    return;
                }
                _viewModel.UsageDieSides = UsageDie.Sizes[pickerUsageDie.SelectedIndex];
                _viewModel.IsStackable = false;
            }

            // === READ PICKER SELECTIONS ===
            if (pickerCategory.SelectedIndex >= 0)
            {
                _viewModel.Category = Enum.Parse<ItemCategory>(_viewModel.Categories[pickerCategory.SelectedIndex]);
            }

            if (pickerSize.SelectedIndex >= 0)
            {
                _viewModel.Size = Enum.Parse<ItemSize>(_viewModel.Sizes[pickerSize.SelectedIndex]);
            }

            _viewModel.Rarity = Math.Max(0, pickerRarity.SelectedIndex);

            // Weapon pickers
            if (pickerWeaponKind.SelectedIndex >= 0)
                _viewModel.WeaponKind = Enum.Parse<WeaponType>(_viewModel.WeaponKinds[pickerWeaponKind.SelectedIndex]);

            if (pickerDamageType.SelectedIndex >= 0)
                _viewModel.DamageType = Enum.Parse<WeaponDamageType>(_viewModel.DamageTypes[pickerDamageType.SelectedIndex]);

            if (pickerWeaponDamage.SelectedIndex >= 0)
            {
                _viewModel.WeaponDamage = Enum.Parse<WeaponDamageDie>(_viewModel.WeaponDamageDice[pickerWeaponDamage.SelectedIndex]);
            }

            if (pickerWeaponSpeed.SelectedIndex >= 0)
            {
                _viewModel.WeaponSpeed = Enum.Parse<WeaponSpeedType>(_viewModel.WeaponSpeeds[pickerWeaponSpeed.SelectedIndex]);
            }

            if (pickerArmorType.SelectedIndex >= 0)
            {
                _viewModel.ArmorType = Enum.Parse<ArmorType>(_viewModel.ArmorTypes[pickerArmorType.SelectedIndex]);
            }

            if (_viewModel.Category == ItemCategory.Container)
            {
                if (!int.TryParse(entryContainerSlots.Text, out int slots) || slots < 1)
                {
                    await DisplayAlertAsync("Container Slots", "Enter how many inventory slots this container provides (1 or more).", "OK");
                    return;
                }
                _viewModel.ContainerSlots = slots;
            }
            else
            {
                _viewModel.ContainerSlots = null;
            }

            if (_viewModel.Category != ItemCategory.Weapon)
            {
                _viewModel.WeaponKind = null;
                _viewModel.DamageType = null;
            }
            else if (_viewModel.WeaponKind == null || _viewModel.DamageType == null)
            {
                await DisplayAlertAsync("Weapon Type", "Choose a Weapon Type and a Damage Type for this weapon.", "OK");
                return;
            }

            // === DEBUG: Verify Name is Set ===
            System.Diagnostics.Debug.WriteLine($"=== Name: '{_viewModel.Name}' ===");
            System.Diagnostics.Debug.WriteLine($"=== Category: '{_viewModel.Category}' ===");

            // Execute create command
            if (_viewModel.CreateCommand.CanExecute(null))
            {
                _viewModel.CreateCommand.Execute(null);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error creating item: {ex.Message}");
            await DisplayAlertAsync("Error", $"Failed to create item: {ex.Message}", "OK");
        }
    }

    private void UpdateContainerVisibility()
    {
        bool isContainer = pickerCategory.SelectedIndex >= 0 &&
            _viewModel.Categories[pickerCategory.SelectedIndex] == nameof(ItemCategory.Container);
        lblContainerHeader.IsVisible = isContainer;
        borderContainer.IsVisible = isContainer;
    }

    // Usage-die items never stack, and Qty Limit only means something on a stackable item.
    private void UpdateUsageDieVisibility()
    {
        bool dieOn = chkUsageDie.IsChecked;
        if (!dieOn) chkHybrid.IsChecked = false;
        usageDieSection.IsVisible = dieOn;
        chkStackable.IsEnabled = !dieOn;

        bool canStack = chkStackable.IsChecked && !dieOn;
        entryQtyLimit.IsEnabled = canStack;
        if (!canStack) entryQtyLimit.Text = string.Empty;
    }

    private void UpdateCategoryHelp()
    {
        string text = pickerCategory.SelectedIndex >= 0
            ? ItemCategoryInfo.Describe(Enum.Parse<ItemCategory>(_viewModel.Categories[pickerCategory.SelectedIndex]))
            : string.Empty;
        lblCategoryHelp.Text = text;
        lblCategoryHelp.IsVisible = text.Length > 0;
    }

    // Ranged weapons are always Piercing, so the picker is set and locked. Thrown weapons default to Piercing but can change.
    private void OnWeaponKindChanged(object? sender, EventArgs e)
    {
        string? kind = pickerWeaponKind.SelectedIndex >= 0 ? _viewModel.WeaponKinds[pickerWeaponKind.SelectedIndex] : null;
        bool ranged = kind == nameof(WeaponType.Ranged);
        bool thrown = kind == nameof(WeaponType.Thrown);

        if (ranged || thrown)
            pickerDamageType.SelectedIndex = _viewModel.DamageTypes.IndexOf(nameof(WeaponDamageType.Piercing));

        pickerDamageType.IsEnabled = !ranged;
        lblRangedNote.IsVisible = ranged;
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}