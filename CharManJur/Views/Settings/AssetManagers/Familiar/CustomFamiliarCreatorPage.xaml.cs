using CharManJur.Helpers;
using CharManJur.Models;
using CharManJur.Helpers;
using CharManJur.Services;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class CustomFamiliarCreatorPage : ContentPage
{
    private readonly IFamiliarDataService _familiarDataService;
    private readonly Action<Familiar> _onFamiliarCreated;
    private readonly Familiar? _familiarBeingEdited;
    private readonly bool _showConfirmation;

    // Raised after the page has finished closing (saved or cancelled).
    public event Action? Closed;

    public bool IsEditMode => _familiarBeingEdited != null;

    public CustomFamiliarCreatorPage(IFamiliarDataService familiarDataService, Action<Familiar> onFamiliarCreated, Familiar? familiarToEdit = null, bool showConfirmation = true)
    {
        InitializeComponent();
        _familiarDataService = familiarDataService;
        _onFamiliarCreated = onFamiliarCreated;
        _familiarBeingEdited = familiarToEdit;
        _showConfirmation = showConfirmation;

        PopulatePickers();

        if (IsEditMode)
        {
            PopulateFormForEdit();
            Title = "Edit Familiar";
            btnSave.Text = "Save Changes";
        }
    }

    private void PopulatePickers()
    {
        // Classes
        pickerClass.ItemsSource = Enum.GetNames(typeof(FmlrClasses)).ToList();

        // Sizes
        pickerSize.ItemsSource = Enum.GetNames(typeof(FmlrSizes)).ToList();

        // Intelligence
        pickerIntelligence.ItemsSource = Enum.GetNames(typeof(FmlrIntelligences)).ToList();

        // Weapon Speeds
        pickerWeaponSpeed.ItemsSource = Enum.GetNames(typeof(FmlrWeaponSpeeds)).ToList();

        // Weapon Damage Dies
        pickerWeaponDamageDie.ItemsSource = Enum.GetNames(typeof(FmlrWeaponDamageDies)).ToList();
    }

    private void PopulateFormForEdit()
    {
        if (_familiarBeingEdited == null) return;

        entryName.Text = _familiarBeingEdited.FmlrName;
        editorDescription.Text = _familiarBeingEdited.FmlrDescription;

        entryHP.Text = _familiarBeingEdited.HP.ToString();
        entryVigor.Text = _familiarBeingEdited.StatVigor.ToString();
        entryAgility.Text = _familiarBeingEdited.StatAgility.ToString();
        entryMind.Text = _familiarBeingEdited.StatMind.ToString();
        entrySpirit.Text = _familiarBeingEdited.StatSpirit.ToString();

        entryBaseCapacity.Text = _familiarBeingEdited.BaseInventoryCapacity.ToString();

        entryWeaponName.Text = _familiarBeingEdited.FmlrWeaponName;

        if (_familiarBeingEdited.FmlrClass.HasValue)
            pickerClass.SelectedIndex = Enum.GetNames(typeof(FmlrClasses)).ToList().IndexOf(_familiarBeingEdited.FmlrClass.Value.ToString());

        pickerSize.SelectedIndex = Enum.GetNames(typeof(FmlrSizes)).ToList().IndexOf(_familiarBeingEdited.FmlrSize.ToString());
        OnSizeSelectedIndexChanged(pickerSize, EventArgs.Empty);

        if (_familiarBeingEdited.FmlrIntelligence.HasValue)
        {
            pickerIntelligence.SelectedIndex = Enum.GetNames(typeof(FmlrIntelligences)).ToList().IndexOf(_familiarBeingEdited.FmlrIntelligence.Value.ToString());
            OnIntelligenceSelectedIndexChanged(pickerIntelligence, EventArgs.Empty);
        }

        if (_familiarBeingEdited.FmlrIntelligence.HasValue)
            pickerIntelligence.SelectedIndex = Enum.GetNames(typeof(FmlrIntelligences)).ToList().IndexOf(_familiarBeingEdited.FmlrIntelligence.Value.ToString());

        if (_familiarBeingEdited.FmlrWeaponSpeed.HasValue)
            pickerWeaponSpeed.SelectedIndex = Enum.GetNames(typeof(FmlrWeaponSpeeds)).ToList().IndexOf(_familiarBeingEdited.FmlrWeaponSpeed.Value.ToString());

        if (_familiarBeingEdited.FmlrWeaponDamageDie.HasValue)
            pickerWeaponDamageDie.SelectedIndex = Enum.GetNames(typeof(FmlrWeaponDamageDies)).ToList().IndexOf(_familiarBeingEdited.FmlrWeaponDamageDie.Value.ToString());

        if (_familiarBeingEdited.Abilities != null && _familiarBeingEdited.Abilities.Any())
        {
            editorAbilities.Text = string.Join("\n", _familiarBeingEdited.Abilities);
        }
    }

    private void OnSizeSelectedIndexChanged(object sender, EventArgs e)
    {
        if (pickerSize.SelectedIndex < 0)
        {
            lblSizeDescription.IsVisible = false;
            return;
        }

        var size = Enum.Parse<FmlrSizes>(pickerSize.SelectedItem?.ToString() ?? "Small");
        string description = size.GetDescription();

        lblSizeDescription.Text = description;
        lblSizeDescription.IsVisible = !string.IsNullOrWhiteSpace(description);
    }

    private void OnIntelligenceSelectedIndexChanged(object sender, EventArgs e)
    {
        if (pickerIntelligence.SelectedIndex < 0)
        {
            lblIntelligenceDescription.IsVisible = false;
            return;
        }

        var intelligence = Enum.Parse<FmlrIntelligences>(pickerIntelligence.SelectedItem?.ToString() ?? "Wild");
        string description = intelligence.GetDescription();

        lblIntelligenceDescription.Text = description;
        lblIntelligenceDescription.IsVisible = !string.IsNullOrWhiteSpace(description);
    }

    private void OnClassSelectedIndexChanged(object sender, EventArgs e)
    {
        bool isConstruct = pickerClass.SelectedItem?.ToString() == nameof(FmlrClasses.Construct);
        capacityStack.IsVisible = isConstruct;

        // A construct has no life force: default it to Inanimate unless an intelligence was already chosen.
        if (isConstruct && pickerIntelligence.SelectedIndex < 0)
        {
            pickerIntelligence.SelectedIndex = Enum.GetNames(typeof(FmlrIntelligences))
                .ToList().IndexOf(nameof(FmlrIntelligences.Inanimate));
        }
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        // Validate
        if (string.IsNullOrWhiteSpace(entryName.Text))
        {
            await DisplayAlertAsync("Error", "Please enter a familiar name.", "OK");
            return;
        }

        if (pickerClass.SelectedIndex < 0)
        {
            await DisplayAlertAsync("Error", "Please select a class/species.", "OK");
            return;
        }

        if (pickerSize.SelectedIndex < 0)
        {
            await DisplayAlertAsync("Error", "Please select a size.", "OK");
            return;
        }

        // Base capacity (constructs only)
        int baseCapacity = 0;
        if (pickerClass.SelectedItem?.ToString() == nameof(FmlrClasses.Construct))
        {
            string capacityText = string.IsNullOrWhiteSpace(entryBaseCapacity.Text) ? "0" : entryBaseCapacity.Text.Trim();
            if (!int.TryParse(capacityText, out baseCapacity) || baseCapacity < 0 || baseCapacity > 99)
            {
                await DisplayAlertAsync("Error", "Base inventory capacity must be a whole number from 0 to 99.", "OK");
                return;
            }
        }

        // Parse stats
        int hp = int.TryParse(entryHP.Text, out int h) ? h : 10;
        int vigor = int.TryParse(entryVigor.Text, out int v) ? v : 10;
        int agility = int.TryParse(entryAgility.Text, out int a) ? a : 10;
        int mind = int.TryParse(entryMind.Text, out int m) ? m : 10;
        int spirit = int.TryParse(entrySpirit.Text, out int s) ? s : 10;

        // Parse weapon
        FmlrWeaponSpeeds? weaponSpeed = null;
        if (pickerWeaponSpeed.SelectedIndex >= 0)
        {
            weaponSpeed = Enum.Parse<FmlrWeaponSpeeds>(pickerWeaponSpeed.SelectedItem?.ToString() ?? "Balanced");
        }

        FmlrWeaponDamageDies? weaponDamageDie = null;
        if (pickerWeaponDamageDie.SelectedIndex >= 0)
        {
            weaponDamageDie = Enum.Parse<FmlrWeaponDamageDies>(pickerWeaponDamageDie.SelectedItem?.ToString() ?? "D4");
        }

        // Parse abilities
        var abilities = new List<string>();
        if (!string.IsNullOrWhiteSpace(editorAbilities.Text))
        {
            abilities = editorAbilities.Text
                .Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(a => a.Trim())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .ToList();
        }

        try
        {
            if (IsEditMode && _familiarBeingEdited != null)
            {
                _familiarBeingEdited.FmlrName = entryName.Text.Trim();
                _familiarBeingEdited.FmlrDescription = editorDescription.Text?.Trim();
                _familiarBeingEdited.FmlrClass = Enum.Parse<FmlrClasses>(pickerClass.SelectedItem?.ToString() ?? "Mammalia");
                _familiarBeingEdited.FmlrSize = Enum.Parse<FmlrSizes>(pickerSize.SelectedItem?.ToString() ?? "Small");
                _familiarBeingEdited.FmlrIntelligence = Enum.Parse<FmlrIntelligences>(pickerIntelligence.SelectedItem?.ToString() ?? "Wild");
                _familiarBeingEdited.HP = hp;
                _familiarBeingEdited.StatVigor = vigor;
                _familiarBeingEdited.StatAgility = agility;
                _familiarBeingEdited.StatMind = mind;
                _familiarBeingEdited.StatSpirit = spirit;
                _familiarBeingEdited.FmlrWeaponName = entryWeaponName.Text?.Trim();
                _familiarBeingEdited.FmlrWeaponSpeed = weaponSpeed;
                _familiarBeingEdited.FmlrWeaponDamageDie = weaponDamageDie;
                _familiarBeingEdited.Abilities = abilities;
                _familiarBeingEdited.BaseInventoryCapacity = baseCapacity;

                await _familiarDataService.UpdateFamiliarAsync(_familiarBeingEdited);
                _onFamiliarCreated?.Invoke(_familiarBeingEdited);

                if (_showConfirmation)
                    await DisplayAlertAsync("Saved!", $"'{_familiarBeingEdited.FmlrName}' updated.", "OK");
            }
            else
            {
                var request = new CreateCustomFamiliarRequest
                {
                    Name = entryName.Text.Trim(),
                    Description = editorDescription.Text?.Trim(),
                    FmlrClass = Enum.Parse<FmlrClasses>(pickerClass.SelectedItem?.ToString() ?? "Mammalia"),
                    FmlrSize = Enum.Parse<FmlrSizes>(pickerSize.SelectedItem?.ToString() ?? "Small"),
                    Intelligence = Enum.Parse<FmlrIntelligences>(pickerIntelligence.SelectedItem?.ToString() ?? "Wild"),
                    HP = hp,
                    StatVigor = vigor,
                    StatAgility = agility,
                    StatMind = mind,
                    StatSpirit = spirit,
                    WeaponName = entryWeaponName.Text?.Trim(),
                    WeaponSpeed = weaponSpeed,
                    WeaponDamageDie = weaponDamageDie,
                    Abilities = abilities,
                    BaseInventoryCapacity = baseCapacity,
                };

                var newFamiliar = await _familiarDataService.CreateCustomFamiliarAsync(request);
                _onFamiliarCreated?.Invoke(newFamiliar);

                if (_showConfirmation)
                    await DisplayAlertAsync("Familiar Created", $"'{newFamiliar.FmlrName}' has been created.", "OK");
            }

            await Navigation.PopModalAsync();
            Closed?.Invoke();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save familiar: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
        Closed?.Invoke();
    }

}