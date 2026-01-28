using System;
using System.Reflection;

namespace Dpz.Core.WebMore;

public partial class Program
{
    /// <summary>
    /// web api base address
    /// </summary>
    public static string BaseAddress { get; private set; } = "";

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

    /// <summary>
    /// assets prefix
    /// </summary>
    public const string AssetsPrefix = "./_content/Dpz.Core.WebMore";

    public static bool UseBlazorMusicPlayer { get; set; }

    internal static void Configure(WebMoreOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.BaseAddress))
        {
            throw new ArgumentException("BaseAddress is null or empty", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.WebHost))
        {
            throw new ArgumentException("WebHost is null or empty", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.AssetsHost))
        {
            throw new ArgumentException("AssetsHost is null or empty", nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.LibraryHost))
        {
            throw new ArgumentException("LibraryHost is null or empty", nameof(options));
        }

        BaseAddress = options.BaseAddress;
        WebHost = options.WebHost;
        AssetsHost = options.AssetsHost;
        LibraryHost = options.LibraryHost;
        UseBlazorMusicPlayer = options.UseBlazorMusicPlayer;
    }
}
