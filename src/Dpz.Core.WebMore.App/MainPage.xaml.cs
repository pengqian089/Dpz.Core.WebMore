using Microsoft.AspNetCore.Components.WebView.Maui;

namespace Dpz.Core.WebMore.App;

public partial class MainPage : ContentPage
{
    public static BlazorWebView? BlazorWebViewInstance { get; private set; }

    public MainPage()
    {
        InitializeComponent();
        BlazorWebViewInstance = blazorWebView;
    }
}