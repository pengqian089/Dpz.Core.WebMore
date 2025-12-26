using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Dpz.Core.WebMore;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");
var configuration = builder.Configuration;
var baseAddress =
    configuration["BaseAddress"]
    ?? throw new Exception("configuration node BaseAddress is null or empty");
WebHost =
    configuration["SourceSite"]
    ?? throw new Exception("configuration node SourceSite is null or empty");
AssetsHost =
    configuration["AssetsHost"]
    ?? throw new Exception("configuration node AssetsHost is null or empty");
LibraryHost =
    configuration["LibraryHost"]
    ?? throw new Exception("configuration node LibraryHost is null or empty");


builder.Services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(baseAddress) });

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
