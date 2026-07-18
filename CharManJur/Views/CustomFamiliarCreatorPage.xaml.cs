using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Views.Godrick_LiveGame;

public partial class CustomFamiliarCreatorPage : ContentPage
{
    private readonly IFamiliarDataService _familiarDataService;
    private readonly Action<Familiar> _onFamiliarCreated;

    public CustomFamiliarCreatorPage(IFamiliarDataService familiarDataService, Action<Familiar> onFamiliarCreated)
    {
        InitializeComponent();
        _familiarDataService = familiarDataService;
        _onFamiliarCreated = onFamiliarCreated;

        PopulatePickers();
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
            Abilities = abilities
        };

        try
        {
            var newFamiliar = await _familiarDataService.CreateCustomFamiliarAsync(request);
            _onFamiliarCreated?.Invoke(newFamiliar);

            await DisplayAlertAsync("Success!", $"Familiar '{newFamiliar.FmlrName}' created with ID {newFamiliar.Id}", "OK");
            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to create familiar: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}