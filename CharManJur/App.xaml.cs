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
            return new Window(new AppShell());
        }
    }
}