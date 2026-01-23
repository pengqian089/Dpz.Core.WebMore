using Dpz.Core.WebMore.App.Pages.Tools;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui;
using Serilog;

namespace Dpz.Core.WebMore.App;

public partial class MainPage : ContentPage
{
    public static BlazorWebView? BlazorWebViewInstance { get; private set; }

    public MainPage()
    {
        InitializeComponent();
        BlazorWebViewInstance = blazorWebView;
    }

    private async void OnOpenNativePlayer(object sender, EventArgs e)
    {
#if ANDROID
        var page = IPlatformApplication.Current?.Services.GetService<NativeMusicPlayerPage>();
        if (page != null)
        {
            await Navigation.PushModalAsync(page);
        }
#endif
    }

    private async void OnOpenLogViewer(object sender, EventArgs e)
    {
        try
        {
            var page = IPlatformApplication.Current?.Services.GetService<LogViewerPage>();
            if (page != null)
            {
                await Navigation.PushAsync(page);
            }
            else
            {
                await DisplayAlertAsync("日志", "日志页面未注册。", "确定");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "打开日志页面失败");
            await DisplayAlertAsync("日志", $"打开日志页面失败：{ex.Message}", "确定");
        }
    }
}