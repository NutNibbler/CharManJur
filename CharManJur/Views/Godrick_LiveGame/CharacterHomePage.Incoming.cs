using CharManJur.ViewModels.Godrick_LiveGame;

namespace CharManJur.Views.Godrick_LiveGame;

// Makes the Apply Damage and Apply Effect buttons blink while the GM has sent something the player hasn't taken on.
public partial class CharacterHomePage
{
    private bool _damageFlashing;
    private bool _effectFlashing;

    private void InitializeIncomingFlash()
    {
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (string.IsNullOrEmpty(e.PropertyName)
                || e.PropertyName == nameof(CharacterHomeViewModel.HasIncomingDamage)
                || e.PropertyName == nameof(CharacterHomeViewModel.HasIncomingEffect))
                MainThread.BeginInvokeOnMainThread(UpdateIncomingFlash);
        };

        Appearing += (_, _) => UpdateIncomingFlash();
    }

    // Starts a button blinking when something is waiting for it. Each blink loop stops itself when nothing is left.
    private void UpdateIncomingFlash()
    {
        if (_viewerClosing) return;

        System.Diagnostics.Debug.WriteLine(
            $"[FLASH] damage waiting={_viewModel.HasIncomingDamage} flashing={_damageFlashing} | effect waiting={_viewModel.HasIncomingEffect} flashing={_effectFlashing}");

        if (_viewModel.HasIncomingDamage && !_damageFlashing)
        {
            _damageFlashing = true;
            _ = FlashAsync(ApplyDamageHost, () => _viewModel.HasIncomingDamage && !_viewerClosing, () => _damageFlashing = false);
        }

        if (_viewModel.HasIncomingEffect && !_effectFlashing)
        {
            _effectFlashing = true;
            _ = FlashAsync(ApplyEffectHost, () => _viewModel.HasIncomingEffect && !_viewerClosing, () => _effectFlashing = false);
        }
    }

    private static async Task FlashAsync(VisualElement target, Func<bool> keepGoing, Action finished)
    {
        try
        {
            while (keepGoing())
            {
                // The page may still be setting up (the GM's viewer starts the flash before it slides in): wait, don't give up.
                if (target.Handler == null)
                {
                    await Task.Delay(250);
                    continue;
                }

                bool cancelled = await target.FadeToAsync(0.35, 650, Easing.SinInOut);
                if (cancelled) { await Task.Delay(200); continue; }   // never spin if the animation can't run

                if (!keepGoing()) break;
                cancelled = await target.FadeToAsync(1, 650, Easing.SinInOut);
                if (cancelled) await Task.Delay(200);
            }
        }
        finally
        {
            target.CancelAnimations();
            target.Opacity = 1;
            finished();
        }
    }
}