using CharManJur.Resources.Styles;
using CharManJur.Services;

namespace CharManJur
{
    public partial class App : Application
    {
        public App(IThemeService themeService)
        {
            InitializeComponent();

            GodrickFixedColors.Register(Application.Current!.Resources);
            themeService.RegisterDefaults(Application.Current!.Resources);
            themeService.ApplyTheme();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            return new Window(new AppShell());
        }
    }
}