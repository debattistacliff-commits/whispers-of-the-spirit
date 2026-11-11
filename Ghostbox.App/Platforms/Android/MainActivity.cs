using Android.App;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.View;

namespace Ghostbox.App;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        // Android 15 draws apps behind the status bar and the navigation buttons. Keep the console
        // between them, on the console's dark background, so nothing is hidden underneath.
        var content = FindViewById(Android.Resource.Id.Content);
        if (content is null) return;
        content.SetBackgroundColor(Android.Graphics.Color.ParseColor("#020508"));
        ViewCompat.SetOnApplyWindowInsetsListener(content, new SystemBarsPadding());
    }

    sealed class SystemBarsPadding : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat OnApplyWindowInsets(Android.Views.View v, WindowInsetsCompat insets)
        {
            var bars = insets.GetInsets(WindowInsetsCompat.Type.SystemBars() | WindowInsetsCompat.Type.DisplayCutout());
            v.SetPadding(bars.Left, bars.Top, bars.Right, bars.Bottom);
            return WindowInsetsCompat.Consumed;
        }
    }
}
