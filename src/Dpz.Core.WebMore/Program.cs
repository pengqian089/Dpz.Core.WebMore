using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using Dpz.Core.WebMore;
using Dpz.Core.WebMore.Helper;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using Serilog;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.TopCenter;
    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    ;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});
var configuration = builder.Configuration;
BaseAddress =
    configuration["BaseAddress"]
    ?? throw new Exception("configuration node BaseAddress is null or empty");
CdnBaseAddress = configuration["CDNBaseAddress"];
WebHost =
    configuration["SourceSite"]
    ?? throw new Exception("configuration node SourceSite is null or empty");
AssetsHost =
    configuration["AssetsHost"]
    ?? throw new Exception("configuration node AssetsHost is null or empty");
LibraryHost =
    configuration["LibraryHost"]
    ?? throw new Exception("configuration node LibraryHost is null or empty");

Connection = new HubConnectionBuilder()
    .WithUrl(
        $"{WebHost}/notification",
        x =>
        {
            x.SkipNegotiation = true;
            x.Transports = HttpTransportType.WebSockets;
        }
    )
    .WithAutomaticReconnect()
    .Build();
try
{
    await Connection.StartAsync();
}
catch (Exception e)
{
    Console.WriteLine(e);
}

Connection.Closed += error =>
{
    Console.WriteLine(error);
    return Task.CompletedTask;
};

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(BaseAddress) });

RegisterInject(builder);

Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .WriteTo.Console()
    .CreateLogger();
builder.Services.AddLogging(loggingBuilder => loggingBuilder.AddSerilog(dispose: true));

await builder.Build().RunAsync();

static void RegisterInject(WebAssemblyHostBuilder builder)
{
    // builder.Services.AddHttpClient(configureClient:client => client.BaseAddress = new Uri(baseAddress));
    // builder.Services.AddScoped<HttpClient>();
    var allTypes = Assembly.GetExecutingAssembly().GetTypes();
    var injectTypes = allTypes.Where(x =>
        x is { Namespace: "Dpz.Core.WebMore.Service", IsInterface: true }
    );
    var implementAssembly = allTypes
        .Where(x =>
            x
                is {
                    Namespace: "Dpz.Core.WebMore.Service.Impl",
                    IsAbstract: false,
                    IsInterface: false
                }
        )
        .ToList();
    foreach (var injectType in injectTypes)
    {
        var defaultImplementType = implementAssembly.FirstOrDefault(x =>
            injectType.IsAssignableFrom(x)
        );
        if (defaultImplementType != null)
        {
            builder.Services.AddScoped(injectType, defaultImplementType);
        }
    }
}

public partial class Program
{
    /// <summary>
    /// web host
    /// </summary>
    public static string WebHost { get; private set; } = "";

    /// <summary>
    /// API base address
    /// </summary>
    public static string BaseAddress { get; private set; } = "";

    /// <summary>
    /// SignalR connection
    /// </summary>
    public static HubConnection Connection { get; private set; }

    /// <summary>
    /// CDN base address
    /// </summary>
    [Obsolete("Use AssetsHost or LibraryHost instead")]
    public static string CdnBaseAddress { get; set; }

    public static string Version =>
        Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "_version";

    /// <summary>
    /// assets host
    /// </summary>
    public static string AssetsHost { get; private set; } = "";

    /// <summary>
    /// library host
    /// </summary>
    public static string LibraryHost { get; private set; } = "";

    /// <summary>
    /// upyun host
    /// </summary>
    public static string UpyunHost => "https://cdn.dpangzi.com";
}
