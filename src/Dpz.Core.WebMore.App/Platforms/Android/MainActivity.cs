using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Webkit;
using AndroidX.Core.App;
using AndroidX.Core.View;
using Dpz.Core.WebMore.Service;
using WebView = Android.Webkit.WebView;

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

        // Request notification permission for Android 13+
        RequestNotificationPermission();
    }

    private void RequestNotificationPermission()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Tiramisu)
        {
            if (
                CheckSelfPermission(Android.Manifest.Permission.PostNotifications)
                != Permission.Granted
            )
            {
                RequestPermissions(new[] { Android.Manifest.Permission.PostNotifications }, 1001);
            }
        }
    }

#pragma warning disable CS0672 // 成员将重写过时的成员
    public override void OnBackPressed()
#pragma warning restore CS0672 // 成员将重写过时的成员
    {
        if (MainPage.BlazorWebViewInstance?.Handler?.PlatformView is WebView webView)
        {
            if (webView.CanGoBack())
            {
                webView.GoBack();
                return;
            }

            webView.EvaluateJavascript(
                "(function(){if(history.length>1){history.back();return '1';}return '0';})()",
                new JsValueCallback(result =>
                {
                    if (result == "1")
                    {
                        return;
                    }
                    HandleExitRequest();
                })
            );
            return;
        }

        HandleExitRequest();
    }

    private void HandleExitRequest()
    {
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
            Finish();
        }
    }

    private sealed class JsValueCallback(Action<string> onResult) : Java.Lang.Object, IValueCallback
    {
        public void OnReceiveValue(Java.Lang.Object? value)
        {
            var str = value?.ToString()?.Trim('"') ?? "0";
            onResult(str);
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
