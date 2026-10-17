using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Views;

public partial class CustomBlueprintCreatorPage : ContentPage
{
    private readonly IBlueprintDataService _blueprintDataService;
    private readonly Action<Blueprint> _onBlueprintCreated;
    private readonly Blueprint? _blueprintBeingEdited;

    // Ambient class/feature context, passed in when opened from inside character
    // creation (the SubFeatureSelectionPopup) so a new Blueprint immediately shows up
    // for the character being built. Left null when opened from the Asset Manager,
    // which makes the new Blueprint universally visible (see BlueprintDataService's
    // wildcard filter logic).
    private readonly int? _contextClassId;
    private readonly int? _contextFeatureId;

    public bool IsEditMode => _blueprintBeingEdited != null;

    public CustomBlueprintCreatorPage(
        IBlueprintDataService blueprintDataService,
        Action<Blueprint> onBlueprintCreated,
        Blueprint? blueprintToEdit = null,
        int? contextClassId = null,
        int? contextFeatureId = null)
    {
        InitializeComponent();
        _blueprintDataService = blueprintDataService;
        _onBlueprintCreated = onBlueprintCreated;
        _blueprintBeingEdited = blueprintToEdit;
        _contextClassId = contextClassId;
        _contextFeatureId = contextFeatureId;

        if (IsEditMode)
        {
            PopulateFormForEdit();
            Title = "Edit Blueprint";
            btnSave.Text = "Save Changes";
        }
    }

    private void PopulateFormForEdit()
    {
        if (_blueprintBeingEdited == null) return;

        entryName.Text = _blueprintBeingEdited.BlueprintName;
        editorDescription.Text = _blueprintBeingEdited.BlueprintDescription;
        entryCost.Text = _blueprintBeingEdited.BlueprintCost.ToString();
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(entryName.Text))
        {
            await DisplayAlertAsync("Error", "Please enter a blueprint name.", "OK");
            return;
        }

        int cost = int.TryParse(entryCost.Text, out int c) ? c : 0;

        try
        {
            if (IsEditMode && _blueprintBeingEdited != null)
            {
                _blueprintBeingEdited.BlueprintName = entryName.Text.Trim();
                _blueprintBeingEdited.BlueprintDescription = editorDescription.Text?.Trim() ?? string.Empty;
                _blueprintBeingEdited.BlueprintCost = cost;

                await _blueprintDataService.UpdateBlueprintAsync(_blueprintBeingEdited);
                _onBlueprintCreated?.Invoke(_blueprintBeingEdited);

                await DisplayAlertAsync("Saved!", $"'{_blueprintBeingEdited.BlueprintName}' updated.", "OK");
            }
            else
            {
                var request = new CreateCustomBlueprintRequest
                {
                    Name = entryName.Text.Trim(),
                    Description = editorDescription.Text?.Trim() ?? string.Empty,
                    Cost = cost,
                    UnlockClassId = _contextClassId,
                    UnlockFeatureId = _contextFeatureId
                };

                var newBlueprint = await _blueprintDataService.CreateCustomBlueprintAsync(request);
                _onBlueprintCreated?.Invoke(newBlueprint);

                await DisplayAlertAsync("Success!", $"Blueprint '{newBlueprint.BlueprintName}' created.", "OK");
            }

            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save blueprint: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}