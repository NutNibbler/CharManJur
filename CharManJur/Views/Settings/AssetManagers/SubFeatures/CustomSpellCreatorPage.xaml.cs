using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Views;

public partial class CustomSpellCreatorPage : ContentPage
{
    private readonly ISpellDataService _spellDataService;
    private readonly Action<Spell> _onSpellCreated;
    private readonly Spell? _spellBeingEdited;

    // Ambient class/feature context, passed in when opened from inside character
    // creation (the SubFeatureSelectionPopup) so a new Spell immediately shows up
    // for the character being built. Left null when opened from the Asset Manager,
    // meaning the new Spell won't appear during character creation until a future
    // live-game unlock system provides another path to it.
    private readonly int? _contextClassId;
    private readonly int? _contextFeatureId;

    public bool IsEditMode => _spellBeingEdited != null;

    public CustomSpellCreatorPage(
        ISpellDataService spellDataService,
        Action<Spell> onSpellCreated,
        Spell? spellToEdit = null,
        int? contextClassId = null,
        int? contextFeatureId = null)
    {
        InitializeComponent();
        _spellDataService = spellDataService;
        _onSpellCreated = onSpellCreated;
        _spellBeingEdited = spellToEdit;
        _contextClassId = contextClassId;
        _contextFeatureId = contextFeatureId;

        if (IsEditMode)
        {
            PopulateFormForEdit();
            Title = "Edit Spell";
            btnSave.Text = "Save Changes";
        }
    }

    private void PopulateFormForEdit()
    {
        if (_spellBeingEdited == null) return;

        entryName.Text = _spellBeingEdited.SpellName;
        editorDescription.Text = _spellBeingEdited.SpellDescription;
        entryDice.Text = _spellBeingEdited.SpellDice;
        entryRange.Text = _spellBeingEdited.SpellRange;
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(entryName.Text))
        {
            await DisplayAlertAsync("Error", "Please enter a spell name.", "OK");
            return;
        }

        try
        {
            if (IsEditMode && _spellBeingEdited != null)
            {
                _spellBeingEdited.SpellName = entryName.Text.Trim();
                _spellBeingEdited.SpellDescription = editorDescription.Text?.Trim();
                _spellBeingEdited.SpellDice = string.IsNullOrWhiteSpace(entryDice.Text) ? null : entryDice.Text.Trim();
                _spellBeingEdited.SpellRange = string.IsNullOrWhiteSpace(entryRange.Text) ? null : entryRange.Text.Trim();

                await _spellDataService.UpdateSpellAsync(_spellBeingEdited);
                _onSpellCreated?.Invoke(_spellBeingEdited);

                await DisplayAlertAsync("Saved!", $"'{_spellBeingEdited.SpellName}' updated.", "OK");
            }
            else
            {
                var request = new CreateCustomSpellRequest
                {
                    Name = entryName.Text.Trim(),
                    Description = editorDescription.Text?.Trim(),
                    Dice = string.IsNullOrWhiteSpace(entryDice.Text) ? null : entryDice.Text.Trim(),
                    Range = string.IsNullOrWhiteSpace(entryRange.Text) ? null : entryRange.Text.Trim(),
                    UnlockClassId = _contextClassId,
                    UnlockFeatureId = _contextFeatureId
                };

                var newSpell = await _spellDataService.CreateCustomSpellAsync(request);
                _onSpellCreated?.Invoke(newSpell);

                await DisplayAlertAsync("Success!", $"Spell '{newSpell.SpellName}' created.", "OK");
            }

            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save spell: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}