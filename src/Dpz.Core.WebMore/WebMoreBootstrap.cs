using System;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Dpz.Core.WebMore;

public sealed class WebMoreOptions
{
    public string BaseAddress { get; set; } = "";
    public string WebHost { get; set; } = "";
    public string AssetsHost { get; set; } = "";
    public string LibraryHost { get; set; } = "";

    public bool UseBlazorMusicPlayer { get; set; } = true;
}

public static class WebMoreServiceCollectionExtensions
{
    public static IServiceCollection AddWebMoreUi(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        var options = new WebMoreOptions
        {
            BaseAddress =
                configuration["BaseAddress"]
                ?? throw new Exception("configuration node BaseAddress is null or empty"),
            WebHost =
                configuration["SourceSite"]
                ?? throw new Exception("configuration node SourceSite is null or empty"),
            AssetsHost =
                configuration["AssetsHost"]
                ?? throw new Exception("configuration node AssetsHost is null or empty"),
            LibraryHost =
                configuration["LibraryHost"]
                ?? throw new Exception("configuration node LibraryHost is null or empty")
        };

        return services.AddWebMoreUi(options);
    }

    public static IServiceCollection AddWebMoreUi(this IServiceCollection services, WebMoreOptions options)
    {
        Program.Configure(options);

        services.AddScoped(_ => new HttpClient { BaseAddress = new Uri(options.BaseAddress) });
        RegisterInject(services);

        return services;
    }

    private static void RegisterInject(IServiceCollection services)
    {
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
                services.AddScoped(injectType, defaultImplementType);
            }
        }
    }
}
