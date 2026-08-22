using CharManJur.Models;
using CharManJur.Services;
using CharManJur.ViewModels;

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

        // Set default category selection to trigger visibility
        pickerCategory.SelectedIndex = 0;

        if (_viewModel.IsEditMode)
        {
            PopulateFormForEdit();
        }
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
        pickerWeaponCategory.ItemsSource = _viewModel.WeaponCategories;
        pickerWeaponDamage.ItemsSource = _viewModel.WeaponDamageDice;
        pickerWeaponSpeed.ItemsSource = _viewModel.WeaponSpeeds;

        // Armor
        pickerArmorType.ItemsSource = _viewModel.ArmorTypes;

        // Weapon Effects
        cvWeaponEffects.ItemsSource = _viewModel.WeaponEffects;
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
            pickerRarity.SelectedIndex = _viewModel.Rarity.Value - 1;

        if (_viewModel.WeaponCategory.HasValue)
            pickerWeaponCategory.SelectedIndex = _viewModel.WeaponCategories.IndexOf(_viewModel.WeaponCategory.Value.ToString());

        if (_viewModel.WeaponDamage.HasValue)
            pickerWeaponDamage.SelectedIndex = _viewModel.WeaponDamageDice.IndexOf(_viewModel.WeaponDamage.Value.ToString());

        if (_viewModel.WeaponSpeed.HasValue)
            pickerWeaponSpeed.SelectedIndex = _viewModel.WeaponSpeeds.IndexOf(_viewModel.WeaponSpeed.Value.ToString());

        if (_viewModel.ArmorType.HasValue)
            pickerArmorType.SelectedIndex = _viewModel.ArmorTypes.IndexOf(_viewModel.ArmorType.Value.ToString());
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

            // === READ PICKER SELECTIONS ===
            if (pickerCategory.SelectedIndex >= 0)
            {
                _viewModel.Category = Enum.Parse<ItemCategory>(_viewModel.Categories[pickerCategory.SelectedIndex]);
            }

            if (pickerSize.SelectedIndex >= 0)
            {
                _viewModel.Size = Enum.Parse<ItemSize>(_viewModel.Sizes[pickerSize.SelectedIndex]);
            }

            if (pickerRarity.SelectedIndex >= 0)
            {
                _viewModel.Rarity = pickerRarity.SelectedIndex + 1;
            }

            // Weapon pickers
            if (pickerWeaponCategory.SelectedIndex >= 0)
            {
                _viewModel.WeaponCategory = Enum.Parse<WeaponCategoryType>(_viewModel.WeaponCategories[pickerWeaponCategory.SelectedIndex]);
            }

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

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}