using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AngleSharp;
using Dpz.Core.WebMore.Helper;
using Markdig;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Shared.Components;

public partial class MarkdownPreview : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public required string Markdown { get; set; }

    [Parameter]
    public string? Style { get; set; }

    [Parameter]
    public string? Class { get; set; } = "markdown-body";

    private string _htmlContent = "";
    private string _lastRenderedMarkdown = "";

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAutoLinks()
        .UsePipeTables()
        .UseTaskLists()
        .UseEmphasisExtras()
        .UseFooters()
        .UseCitations()
        .UseMathematics()
        .UseAutoIdentifiers()
        .Build();

    private static readonly Dictionary<string, string> Cache = new();

    /// <summary>
    /// 限制缓存大小，防止内存泄漏
    /// </summary>
    private const int MaxCacheSize = 1 << 10;

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrWhiteSpace(Markdown))
        {
            _htmlContent = "";
            _lastRenderedMarkdown = "";
            return;
        }

        // 内容没有改变，跳过处理
        if (_lastRenderedMarkdown == Markdown)
        {
            return;
        }

        // 使用内容的哈希值作为缓存 key
        var cacheKey = GetCacheKey(Markdown);

        if (Cache.TryGetValue(cacheKey, out var html))
        {
            _htmlContent = html;
        }
        else
        {
            var parseHtml = await ParseMarkdownAsync();

            // 如果缓存已满，清除最早的条目（简单的 FIFO 策略）
            if (Cache.Count >= MaxCacheSize)
            {
                var firstKey = Cache.Keys.FirstOrDefault();
                if (firstKey != null)
                {
                    Cache.Remove(firstKey);
                }
            }

            Cache[cacheKey] = parseHtml;
            _htmlContent = parseHtml;
        }

        await base.OnParametersSetAsync();
    }

    protected override void OnAfterRender(bool firstRender)
    {
        // 在渲染完成后更新，确保下次能正确判断是否需要重新渲染
        _lastRenderedMarkdown = Markdown;
        base.OnAfterRender(firstRender);
    }

    private static string GetCacheKey(string markdown)
    {
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(markdown));
        return Convert.ToHexString(hashBytes);
    }

    private async Task<string> ParseMarkdownAsync()
    {
        var html = Markdig.Markdown.ToHtml(Markdown, Pipeline);

        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(y => y.Content(html));
        var links = document.GetElementsByTagName("a");
        links.ForEach(y =>
        {
            var href = y.GetAttribute("href");
            if (
                href != null
                && !href.StartsWith("javascript", StringComparison.CurrentCultureIgnoreCase)
            )
            {
                y.SetAttribute("target", "_blank");
            }
        });
        var images = document.GetElementsByTagName("img");
        images.ForEach(y =>
        {
            var src = y.GetAttribute("src");
            if (string.IsNullOrWhiteSpace(src) || src == $"{Program.LibraryHost}/loaders/oval.svg")
            {
                return;
            }
            y.SetAttribute("data-src", src);
            y.SetAttribute("class", "lazy");
            y.SetAttribute("src", $"{Program.LibraryHost}/loaders/oval.svg");
        });
        return document.Body?.InnerHtml ?? "";
    }
}
