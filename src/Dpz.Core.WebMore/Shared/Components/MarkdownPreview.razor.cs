using System;
using System.Collections.Concurrent;
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

    private static readonly ConcurrentDictionary<string, string> Cache = new();

    protected override bool ShouldRender()
    {
        // 只有当 Markdown 内容改变时才重新渲染
        return Cache.ContainsKey(Markdown);
    }

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrWhiteSpace(Markdown))
        {
            _htmlContent = "";
            return;
        }
        if (Cache.TryGetValue(Markdown, out var html))
        {
            _htmlContent = html;
        }
        else
        {
            var parseHtml = await ParseMarkdownAsync();
            Cache.TryAdd(Markdown, parseHtml);
            _htmlContent = parseHtml;
        }

        await base.OnParametersSetAsync();
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
