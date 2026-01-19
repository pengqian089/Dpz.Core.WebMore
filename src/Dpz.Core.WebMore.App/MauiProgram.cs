using Dpz.Core.WebMore;
using Microsoft.Extensions.Logging;

namespace Dpz.Core.WebMore.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => { fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular"); });

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddWebMoreUi(
            new WebMoreOptions
            {
                BaseAddress = "https://api.dpangzi.com",
                WebHost = "https://core.dpangzi.com",
                AssetsHost = "https://assets.dpangzi.com/core",
                LibraryHost = "https://dpangzi.com"
            }
        );

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}