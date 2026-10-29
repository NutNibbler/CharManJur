using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

// Makes the Bio button pulse while there are training points to spend.
public partial class CharacterHomePage
{
    private bool _bioFlashOn;
    private int _bioFlashToken;

    private void InitializeBioFlash()
    {
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(CharacterHomeViewModel.HasBioAlert)) UpdateBioFlash();
        };
        UpdateBioFlash();
    }

    private void UpdateBioFlash()
    {
        bool want = _viewModel.HasBioAlert;
        if (want == _bioFlashOn) return;   // reloads raise this often; only a real change restarts the pulse

        _bioFlashOn = want;
        int token = ++_bioFlashToken;

        if (want)
        {
            _ = RunBioFlashAsync(token);
        }
        else
        {
            BioButtonHost.CancelAnimations();
            BioButtonHost.Opacity = 1;
        }
    }

    private async Task RunBioFlashAsync(int token)
    {
        while (token == _bioFlashToken)
        {
            await BioButtonHost.FadeToAsync(0.35, 650, Easing.SinInOut);
            if (token != _bioFlashToken) break;
            await BioButtonHost.FadeToAsync(1, 650, Easing.SinInOut);
        }

        if (token == _bioFlashToken) BioButtonHost.Opacity = 1;
    }
}