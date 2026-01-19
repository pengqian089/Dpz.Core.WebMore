using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using AndroidX.Core.View;

namespace Dpz.Core.WebMore.App;

[Activity(
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    ConfigurationChanges = ConfigChanges.ScreenSize
        | ConfigChanges.Orientation
        | ConfigChanges.UiMode
        | ConfigChanges.ScreenLayout
        | ConfigChanges.SmallestScreenSize
        | ConfigChanges.Density
)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        WindowCompat.SetDecorFitsSystemWindows(Window, false);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
        {
            Window?.Attributes?.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
        }

        var content = Window?.DecorView?.FindViewById(Android.Resource.Id.Content);
        if (content != null)
        {
            ViewCompat.SetOnApplyWindowInsetsListener(content, new InsetsListener());
        }
    }

    private sealed class InsetsListener : Java.Lang.Object, IOnApplyWindowInsetsListener
    {
        public WindowInsetsCompat? OnApplyWindowInsets(
            Android.Views.View? v,
            WindowInsetsCompat? insets
        )
        {
            var systemBars = insets?.GetInsets(WindowInsetsCompat.Type.SystemBars());
            if (systemBars == null)
            {
                return null;
            }
            v?.SetPadding(systemBars.Left, systemBars.Top, systemBars.Right, systemBars.Bottom);
            return insets;
        }
    }
}
