using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using AndroidX.Core.View;
using Dpz.Core.WebMore.Service;

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
    private DateTime _lastBackPressedUtc = DateTime.MinValue;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        WindowCompat.SetDecorFitsSystemWindows(Window, false);

        if (Build.VERSION.SdkInt >= BuildVersionCodes.P)
        {
#pragma warning disable CA1416
            Window?.Attributes?.LayoutInDisplayCutoutMode = LayoutInDisplayCutoutMode.ShortEdges;
#pragma warning restore CA1416
        }

        var content = Window?.DecorView.FindViewById(Android.Resource.Id.Content);
        if (content != null)
        {
            ViewCompat.SetOnApplyWindowInsetsListener(content, new InsetsListener());
        }
    }

#pragma warning disable CS0672 // 成员将重写过时的成员
    public override void OnBackPressed()
#pragma warning restore CS0672 // 成员将重写过时的成员
    {
        var webView =
            MainPage.BlazorWebViewInstance?.Handler?.PlatformView as Android.Webkit.WebView;
        if (webView?.CanGoBack() == true)
        {
            webView.GoBack();
            return;
        }

        var now = DateTime.UtcNow;
        if ((now - _lastBackPressedUtc).TotalSeconds <= 2)
        {
            Finish();
            return;
        }

        _lastBackPressedUtc = now;
        var dialogService = IPlatformApplication.Current?.Services.GetService<IAppDialogService>();
        if (dialogService != null)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                dialogService.Toast("再次返回退出程序");
            });
        }
        else
        {
#pragma warning disable CS0612 // 类型或成员已过时
            base.OnBackPressed();
#pragma warning restore CS0612 // 类型或成员已过时
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
