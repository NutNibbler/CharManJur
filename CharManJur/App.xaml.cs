using CharManJur.Controls;
using CharManJur.Resources.Styles;
using CharManJur.Services;

namespace CharManJur
{
    public partial class App : Application
    {
        public App(IThemeService themeService, ISkinService skinService)
        {
            InitializeComponent();

            GodrickFixedColors.Register(Application.Current!.Resources);
            themeService.RegisterDefaults(Application.Current!.Resources);
            themeService.ApplyTheme();

            // Scans Data\Skins (creating it if needed) and restores the saved load order and font choice
            skinService.Initialize();

            // Lets the page_background skin image paint behind pages (Windows)
            SkinBackground.Register(skinService);

            // Uses the font chosen in Settings > Skins > Text (needs a restart to change)
            SkinFonts.Register(skinService);
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            bool asked = false;

            window.HandlerChanged += (_, _) => UiScale.Apply(window);
            UiScale.Changed += () => MainThread.BeginInvokeOnMainThread(() => UiScale.Apply(window));
            window.Activated += async (_, _) =>
            {
                UiScale.Apply(window);
                if (asked) return;
                asked = true;

                // A scale that was applied but never confirmed (e.g. the UI was unusable): ask again
                if (UiScale.IsPending)
                {
                    await Task.Delay(600);
                    await UiScale.AskKeepAsync(Shell.Current);
                }
            };
            return window;
        }
    }
}