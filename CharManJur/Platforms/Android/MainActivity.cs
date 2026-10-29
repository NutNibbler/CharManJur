using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Content;
using Android.Content.Res;

namespace CharManJur
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void AttachBaseContext(Context? @base)
        {
            if (@base != null)
            {
                try
                {
                    // MAUI Preferences live in this SharedPreferences file
                    var prefs = @base.GetSharedPreferences(
                        @base.PackageName + ".microsoft.maui.essentials.preferences", FileCreationMode.Private);
                    int percent = Math.Clamp(prefs?.GetInt(CharManJur.Services.UiScale.PrefKey, 100) ?? 100,
                                             CharManJur.Services.UiScale.Min, CharManJur.Services.UiScale.Max);

                    if (percent != 100)
                    {
                        var config = new Configuration(@base.Resources!.Configuration);
                        config.DensityDpi = (int)Math.Round(config.DensityDpi * percent / 100.0);
                        @base = @base.CreateConfigurationContext(config);
                    }
                }
                catch { /* never block startup over a display preference */ }
            }
            base.AttachBaseContext(@base);
        }
    }
}
