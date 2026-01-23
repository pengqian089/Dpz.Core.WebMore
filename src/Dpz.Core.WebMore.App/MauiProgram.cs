using Dpz.Core.WebMore;
using Dpz.Core.WebMore.App.Pages.Tools;
using Dpz.Core.WebMore.Service;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

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

        builder.Services.AddTransient<NativeMusicPlayerPage>();
        builder.Services.AddTransient<LogViewerPage>();

#if ANDROID
        builder.Services.AddScoped<
            IMusicPlayerService,
            Platforms.Android.AndroidMusicPlayerService
        >();
#endif

        var logDirectory = Path.Combine(FileSystem.AppDataDirectory, "logs");
        Directory.CreateDirectory(logDirectory);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(logDirectory, "app-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 14,
                fileSizeLimitBytes: 5_000_000,
                rollOnFileSizeLimit: true,
                shared: true
            )
            .CreateLogger();

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
#endif

        return builder.Build();
    }
}
