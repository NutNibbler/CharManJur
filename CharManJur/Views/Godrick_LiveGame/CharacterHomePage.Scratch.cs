using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

// The Scratch Pad panel: wiring and saving.
public partial class CharacterHomePage
{
    private ScratchPadViewModel? _scratchVm;

    private void InitializeScratchPad()
    {
        _scratchVm = new ScratchPadViewModel();
        ScratchPanel.BindingContext = _scratchVm;

        // Another page opening over the dashboard, or the dashboard going away, saves what was typed.
        _viewModel.ModalOpening += () => _ = SaveScratchPadAsync();
        Disappearing += (_, _) => _ = SaveScratchPadAsync();
    }

    private async void OnScratchPadClicked(object? sender, EventArgs e)
        => await TogglePanelAsync(OverlayPanel.Scratch);

    private Task OpenScratchPadAsync()
        => _scratchVm == null ? Task.CompletedTask : _scratchVm.OpenAsync(_viewModel.CharDataService.CharacterGuid);

    // Cheap when nothing changed.
    private Task SaveScratchPadAsync()
        => _scratchVm == null ? Task.CompletedTask : _scratchVm.SaveAsync();
}