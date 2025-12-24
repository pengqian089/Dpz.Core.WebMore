using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Web;
using Markdig;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;

namespace Dpz.Core.WebMore.Helper;

public static class AppTools
{
    private static readonly Dictionary<string, object> ClientCache = new();
    public static ILogger<Program> ProgramLogger { get; set; }

    /// <summary>
    /// 客户端最大读取文件大小 unit byte
    /// </summary>
    public const long MaxFileSize = 1024 * 1024 * 100;

    public static bool IsDark;

    private static string HandleParameter(string url, Dictionary<string, string>? parameters)
    {
        if (parameters is not { Count: > 0 })
        {
            return url;
        }

        var index = url.IndexOf("?", StringComparison.CurrentCultureIgnoreCase);
        var query = index >= 0 ? url.Substring(index) : "";
        var queryString = HttpUtility.ParseQueryString(query);
        foreach (var item in parameters)
        {
            queryString.Add(item.Key, item.Value);
        }

        if (index >= 0)
        {
            url = url.Substring(0, index + 1) + queryString;
        }
        else
        {
            url += "?" + queryString;
        }

        return url;
    }

    public static async Task<IPagedList<T>> ToPagedListAsync<T>(
        this HttpClient client,
        string url,
        Dictionary<string, string>? parameters = null,
        JsonConverter? converter = null
    )
    {
        var requestUrl = HandleParameter(url, parameters);
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        var currentUri = request.RequestUri?.ToString() ?? "";

        var response = await client.SendAsync(request);

        if (
            response.StatusCode == HttpStatusCode.NotModified
            && ClientCache.TryGetValue(currentUri, out var value)
        )
        {
            return value as PagedList<T> ?? new PagedList<T>(new List<T>(), 0, 0);
        }

        var serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        if (converter != null)
        {
            serializerOptions.Converters.Add(converter);
        }

        var list = await response.Content.ReadFromJsonAsync<List<T>>(serializerOptions) ?? [];
        var pagination =
            JsonSerializer.Deserialize<Pagination>(
                response.Headers.GetValues("X-Pagination").First(),
                serializerOptions
            ) ?? new Pagination();

        var pagedList = new PagedList<T>(
            list,
            pagination.CurrentPage,
            pagination.PageSize,
            pagination.TotalCount
        );
        ClientCache[currentUri] = pagedList;
        return pagedList;
    }

    public static T GetQueryString<T>(this NavigationManager navManager, string key)
    {
        var uri = navManager.ToAbsoluteUri(navManager.Uri);

        var valueFromQueryString = HttpUtility.ParseQueryString(uri.Query).Get(key);
        if (valueFromQueryString != null)
        {
            if (typeof(T) == typeof(int) && int.TryParse(valueFromQueryString, out var valueAsInt))
            {
                return (T)(object)valueAsInt;
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)valueFromQueryString;
            }

            if (
                typeof(T) == typeof(decimal)
                && decimal.TryParse(valueFromQueryString, out var valueAsDecimal)
            )
            {
                return (T)(object)valueAsDecimal;
            }
        }

        return default(T);
    }

    public static string TimeAgo(this DateTime time)
    {
        var ts = new TimeSpan(DateTime.UtcNow.Ticks - time.ToUniversalTime().Ticks);
        var delta = Math.Abs(ts.TotalSeconds);

        switch (delta)
        {
            case < 60:
                return ts.Seconds == 1 ? "刚刚" : ts.Seconds + "秒前";
            case < 60 * 2:
                return "1分钟前";
            case < 45 * 60:
                return ts.Minutes + "分钟前";
            case < 90 * 60:
                return "1小时前";
            case < 24 * 60 * 60:
                return ts.Hours + "小时前";
            case < 48 * 60 * 60:
                return "昨天";
            case < 30 * 24 * 60 * 60:
                return $"{ts.Days}天前";
            case < 12 * 30 * 24 * 60 * 60:
            {
                var months = Convert.ToInt32(Math.Floor((double)ts.Days / 30));
                return months <= 1 ? "一个月前" : $"{months}个月前";
            }
            default:
            {
                var years = Convert.ToInt32(Math.Floor((double)ts.Days / 365));
                return years <= 1 ? "1年前" : $"{years}年前";
            }
        }
    }

    public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
    {
        foreach (var item in source)
        {
            action(item);
        }
    }

    public static async Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> func)
    {
        foreach (var item in source)
        {
            await func(item);
        }
    }

    /// <summary>
    /// Markdown转为Html
    /// </summary>
    /// <param name="markdown"></param>
    /// <param name="disableHtml">是否禁用html（默认禁用）</param>
    /// <returns></returns>
    public static string MarkdownToHtml(this string markdown, bool disableHtml = true)
    {
        var pipelineBuild = new MarkdownPipelineBuilder()
            .UseAutoLinks()
            .UsePipeTables()
            .UseTaskLists()
            .UseEmphasisExtras()
            .UseAutoIdentifiers();

        if (disableHtml)
        {
            pipelineBuild.DisableHtml();
        }

        var pipeline = pipelineBuild.Build();

        var document = Markdown.Parse(markdown, pipeline);
        foreach (var link in document.Descendants<LinkInline>())
        {
            link.GetAttributes().AddPropertyIfNotExist("target", "_blank");
        }

        foreach (var link in document.Descendants<AutolinkInline>())
        {
            link.GetAttributes().AddPropertyIfNotExist("target", "_blank");
        }

        return document.ToHtml(pipeline);
    }

#if DEBUG
    public static void WriteLine(string format, params object?[] args)
    {
        var array = args.Select(x => x is null ? "NULL" : JsonSerializer.Serialize(x)).ToArray();
        Console.WriteLine(format, array);
    }

    public static void WriteLine(this object obj)
    {
        var json = JsonSerializer.Serialize(
            obj,
            new JsonSerializerOptions { WriteIndented = true }
        );
        Console.WriteLine(json);
    }
#endif
}
