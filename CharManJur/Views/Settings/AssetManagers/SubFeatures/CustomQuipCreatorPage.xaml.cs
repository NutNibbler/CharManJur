using CharManJur.Models;
using CharManJur.Services;

namespace CharManJur.Views;

public partial class CustomQuipCreatorPage : ContentPage
{
    private readonly IQuipDataService _quipDataService;
    private readonly Action<Quip> _onQuipCreated;
    private readonly Quip? _quipBeingEdited;
    private readonly int? _contextClassId;
    private readonly int? _contextFeatureId;

    public bool IsEditMode => _quipBeingEdited != null;

    public CustomQuipCreatorPage(
        IQuipDataService quipDataService,
        Action<Quip> onQuipCreated,
        Quip? quipToEdit = null,
        int? contextClassId = null,
        int? contextFeatureId = null)
    {
        InitializeComponent();
        _quipDataService = quipDataService;
        _onQuipCreated = onQuipCreated;
        _quipBeingEdited = quipToEdit;
        _contextClassId = contextClassId;
        _contextFeatureId = contextFeatureId;

        if (IsEditMode)
        {
            entryName.Text = _quipBeingEdited!.QuipName;
            editorDescription.Text = _quipBeingEdited.QuipDescription;
            Title = "Edit Quip";
            btnSave.Text = "Save Changes";
        }
    }

    private async void OnCreateClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(entryName.Text))
        {
            await DisplayAlertAsync("Error", "Please enter a quip name.", "OK");
            return;
        }

        try
        {
            if (IsEditMode && _quipBeingEdited != null)
            {
                _quipBeingEdited.QuipName = entryName.Text.Trim();
                _quipBeingEdited.QuipDescription = editorDescription.Text?.Trim();

                await _quipDataService.UpdateQuipAsync(_quipBeingEdited);
                _onQuipCreated?.Invoke(_quipBeingEdited);

                await DisplayAlertAsync("Saved!", $"'{_quipBeingEdited.QuipName}' updated.", "OK");
            }
            else
            {
                var request = new CreateCustomQuipRequest
                {
                    Name = entryName.Text.Trim(),
                    Description = editorDescription.Text?.Trim(),
                    UnlockClassId = _contextClassId,
                    UnlockFeatureId = _contextFeatureId
                };

                var newQuip = await _quipDataService.CreateCustomQuipAsync(request);
                _onQuipCreated?.Invoke(newQuip);

                await DisplayAlertAsync("Success!", $"Quip '{newQuip.QuipName}' created.", "OK");
            }

            await Navigation.PopModalAsync();
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("Error", $"Failed to save quip: {ex.Message}", "OK");
        }
    }

    private async void OnCancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
    }
}