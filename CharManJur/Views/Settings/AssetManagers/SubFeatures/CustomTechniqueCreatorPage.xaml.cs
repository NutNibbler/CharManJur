using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Views;

public partial class CustomTechniqueCreatorPage : ContentPage
{
    private readonly ITechniqueDataService _techniqueDataService;
    private readonly Action<Technique> _onTechniqueCreated;
    private readonly Technique? _techniqueBeingEdited;
    private readonly int? _contextClassId;
    private readonly int? _contextFeatureId;

    public bool IsEditMode => _techniqueBeingEdited != null;

    public CustomTechniqueCreatorPage(
        ITechniqueDataService techniqueDataService,
        Action<Technique> onTechniqueCreated,
        Technique? techniqueToEdit = null,
        int? contextClassId = null,
        int? contextFeatureId = null)
    {
        InitializeComponent();
        _techniqueDataService = techniqueDataService;
        _onTechniqueCreated = onTechniqueCreated;
        _techniqueBeingEdited = techniqueToEdit;
        _contextClassId = contextClassId;
        _contextFeatureId = contextFeatureId;

        if (IsEditMode)
        {
            entryName.Text = _techniqueBeingEdited!.TechniqueName;
            editorDescription.Text = _techniqueBeingEdited.TechniqueDescription;
            Title = "Edit Technique";
            btnSave.Text = "Save Changes";
        }
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(entryName.Text))
        {
            await DisplayAlertAsync("Error", "Please enter a technique name.", "OK");
            return;
        }

        try
        {
            if (IsEditMode && _techniqueBeingEdited != null)
            {
                _techniqueBeingEdited.TechniqueName = entryName.Text.Trim();
                _techniqueBeingEdited.TechniqueDescription = editorDescription.Text?.Trim() ?? string.Empty;

                await _techniqueDataService.UpdateTechniqueAsync(_techniqueBeingEdited);
                _onTechniqueCreated?.Invoke(_techniqueBeingEdited);

                await DisplayAlertAsync("Saved!", $"'{_techniqueBeingEdited.TechniqueName}' updated.", "OK");
            }
            else
            {
                var request = new CreateCustomTechniqueRequest
                {
                    Name = entryName.Text.Trim(),
                    Description = editorDescription.Text?.Trim() ?? string.Empty,
                    UnlockClassId = _contextClassId,
                    UnlockFeatureId = _contextFeatureId ?? 0
                };

                var newTechnique = await _techniqueDataService.CreateCustomTechniqueAsync(request);
                _onTechniqueCreated?.Invoke(newTechnique);

                await DisplayAlertAsync("Success!", $"Technique '{newTechnique.TechniqueName}' created.", "OK");
            }

            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save technique: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}