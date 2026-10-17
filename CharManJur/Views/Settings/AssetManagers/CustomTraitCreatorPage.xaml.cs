using System.ComponentModel.DataAnnotations;
using System.Reflection;
using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Views;

public partial class CustomTraitCreatorPage : ContentPage
{
    private static readonly string[] PdOptions =
    {
        "1 Polymorph Die (PD 1 list)",
        "2 Polymorph Dice (PD 2 list)",
        "3 Polymorph Dice (PD 3 list)",
        "4 Polymorph Dice (PD 4 list)"
    };

    private static readonly int[] DieSizes = { 4, 6, 8, 10, 12 };

    // Index 0 = no element, 1-6 = a fixed element (matches DamageElement), 7 = the player chooses.
    private static readonly string[] ElementOptions =
        { "None", "Ice", "Fire", "Acid", "Wind", "Earth", "Lightning", "Player chooses" };
    private const int ChooseElementIndex = 7;

    private readonly ITraitDataService _traitDataService;
    private readonly Action<CustomTrait> _onSaved;
    private readonly CustomTrait? _editing;
    private readonly WeaponCategoryType[] _typeValues = Enum.GetValues<WeaponCategoryType>();

    public bool IsEditMode => _editing != null;

    public CustomTraitCreatorPage(ITraitDataService traitDataService, Action<CustomTrait> onSaved, CustomTrait? traitToEdit = null)
    {
        InitializeComponent();
        _traitDataService = traitDataService;
        _onSaved = onSaved;
        _editing = traitToEdit;

        pickerPD.ItemsSource = PdOptions;
        pickerPD.SelectedIndex = 0;

        pickerDie.ItemsSource = DieSizes.Select(d => $"d{d}").ToList();
        pickerDie.SelectedIndex = 1;

        var typeNames = new List<string> { "None" };
        typeNames.AddRange(_typeValues.Select(t => DisplayName(t)));
        pickerType.ItemsSource = typeNames;
        pickerType.SelectedIndex = 0;

        pickerElement.ItemsSource = ElementOptions;
        pickerElement.SelectedIndex = 0;

        if (IsEditMode) LoadFromTrait(_editing!);
    }

    private void LoadFromTrait(CustomTrait trait)
    {
        Title = "Edit Trait";
        btnSave.Text = "Save Changes";

        entryName.Text = trait.TraitName;
        editorEffect.Text = trait.Effect;
        pickerPD.SelectedIndex = Math.Clamp(trait.PD, 1, 4) - 1;

        entryVigor.Text = trait.Vigor?.ToString() ?? string.Empty;
        entryAgility.Text = trait.Agility?.ToString() ?? string.Empty;
        entryArmor.Text = trait.Armor.ToString();

        chkSmall.IsChecked = trait.IsSmall;
        chkLarge.IsChecked = trait.IsLarge;
        chkRequiresSmall.IsChecked = trait.RequiresSmall;
        chkForbidsLarge.IsChecked = trait.ForbidsLarge;
        chkPack.IsChecked = trait.GrantsPack;

        if (trait.WeaponDie.HasValue)
        {
            switchWeapon.IsToggled = true;
            int dieIndex = Array.IndexOf(DieSizes, trait.WeaponDie.Value);
            pickerDie.SelectedIndex = dieIndex >= 0 ? dieIndex : 1;
            pickerType.SelectedIndex = trait.WeaponType.HasValue ? Array.IndexOf(_typeValues, trait.WeaponType.Value) + 1 : 0;
            pickerElement.SelectedIndex = trait.ChooseElement ? ChooseElementIndex : (int)trait.Element;
            chkBlast.IsChecked = trait.Blast;
        }
    }

    private void OnWeaponToggled(object sender, ToggledEventArgs e) => weaponSection.IsVisible = e.Value;

    private static string DisplayName<T>(T value) where T : struct, Enum
    {
        string name = value.ToString();
        var attribute = typeof(T).GetField(name)?.GetCustomAttribute<DisplayAttribute>();
        return attribute?.Name ?? name;
    }

    private static bool TryParseOptionalScore(string? text, string label, out int? value, out string error)
    {
        value = null;
        error = string.Empty;
        if (string.IsNullOrWhiteSpace(text)) return true;

        if (!int.TryParse(text.Trim(), out int parsed) || parsed < 1 || parsed > 30)
        {
            error = $"{label} must be a whole number from 1 to 30 (or left blank).";
            return false;
        }
        value = parsed;
        return true;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(entryName.Text))
        {
            await DisplayAlertAsync("Error", "Please enter a trait name.", "OK");
            return;
        }
        if (pickerPD.SelectedIndex < 0)
        {
            await DisplayAlertAsync("Error", "Choose how many Polymorph Dice this trait needs.", "OK");
            return;
        }

        if (!TryParseOptionalScore(entryVigor.Text, "Form VIG", out int? vigor, out string vigorError))
        {
            await DisplayAlertAsync("Error", vigorError, "OK");
            return;
        }
        if (!TryParseOptionalScore(entryAgility.Text, "Form AGI", out int? agility, out string agilityError))
        {
            await DisplayAlertAsync("Error", agilityError, "OK");
            return;
        }

        int armor = 0;
        if (!string.IsNullOrWhiteSpace(entryArmor.Text) && (!int.TryParse(entryArmor.Text.Trim(), out armor) || armor < 0 || armor > 3))
        {
            await DisplayAlertAsync("Error", "Armor must be a whole number from 0 to 3.", "OK");
            return;
        }

        int pd = pickerPD.SelectedIndex + 1;

        // Natural weapon
        int? weaponDie = null;
        WeaponCategoryType? weaponType = null;
        bool blast = false;
        var element = DamageElement.None;
        bool chooseElement = false;

        if (switchWeapon.IsToggled)
        {
            weaponDie = pickerDie.SelectedIndex >= 0 ? DieSizes[pickerDie.SelectedIndex] : 6;
            weaponType = pickerType.SelectedIndex > 0 ? _typeValues[pickerType.SelectedIndex - 1] : null;
            blast = chkBlast.IsChecked;

            int elementIndex = Math.Max(0, pickerElement.SelectedIndex);
            if (elementIndex == ChooseElementIndex) chooseElement = true;
            else element = (DamageElement)elementIndex;
        }

        try
        {
            if (IsEditMode && _editing != null)
            {
                _editing.TraitName = entryName.Text.Trim();
                _editing.PD = pd;
                _editing.Effect = editorEffect.Text?.Trim() ?? string.Empty;
                _editing.Vigor = vigor;
                _editing.Agility = agility;
                _editing.Armor = armor;
                _editing.WeaponDie = weaponDie;
                _editing.WeaponType = weaponType;
                _editing.Blast = blast;
                _editing.Element = element;
                _editing.ChooseElement = chooseElement;
                _editing.IsSmall = chkSmall.IsChecked;
                _editing.IsLarge = chkLarge.IsChecked;
                _editing.RequiresSmall = chkRequiresSmall.IsChecked;
                _editing.ForbidsLarge = chkForbidsLarge.IsChecked;
                _editing.GrantsPack = chkPack.IsChecked;

                await _traitDataService.UpdateTraitAsync(_editing);
                _onSaved?.Invoke(_editing);

                await DisplayAlertAsync("Saved!", $"'{_editing.TraitName}' updated.", "OK");
            }
            else
            {
                var created = await _traitDataService.CreateCustomTraitAsync(new CreateCustomTraitRequest
                {
                    Name = entryName.Text.Trim(),
                    PD = pd,
                    Effect = editorEffect.Text?.Trim() ?? string.Empty,
                    Vigor = vigor,
                    Agility = agility,
                    Armor = armor,
                    WeaponDie = weaponDie,
                    WeaponType = weaponType,
                    Blast = blast,
                    Element = element,
                    ChooseElement = chooseElement,
                    IsSmall = chkSmall.IsChecked,
                    IsLarge = chkLarge.IsChecked,
                    RequiresSmall = chkRequiresSmall.IsChecked,
                    ForbidsLarge = chkForbidsLarge.IsChecked,
                    GrantsPack = chkPack.IsChecked
                });
                _onSaved?.Invoke(created);

                await DisplayAlertAsync("Success!", $"Trait '{created.TraitName}' created.", "OK");
            }

            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save trait: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}