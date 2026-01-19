using Dpz.Core.WebMore;
using Dpz.Core.WebMore.Service;
using Microsoft.Extensions.Logging;

namespace Dpz.Core.WebMore.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        builder.Services.AddWebMoreUi(
            new WebMoreOptions
            {
#if DEBUG
                BaseAddress = "https://localhost:53381",
                WebHost = "https://localhost:37701",
                AssetsHost = "https://localhost:5505/core",
                LibraryHost = "https://localhost:5505"
#else
                BaseAddress = "https://api.dpangzi.com",
                WebHost = "https://core.dpangzi.com",
                AssetsHost = "https://assets.dpangzi.com/core",
                LibraryHost = "https://dpangzi.com"
#endif
            }
        );

        #if ANDROID
            builder.Services.AddScoped<IMusicPlayerService, Platforms.Android.AndroidMusicPlayerService>();
        #endif

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
