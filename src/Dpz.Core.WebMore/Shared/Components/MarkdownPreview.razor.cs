using System;
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

    protected override async Task OnParametersSetAsync()
    {
        var pipeline = new MarkdownPipelineBuilder()
            .UseAutoLinks()
            .UsePipeTables()
            .UseTaskLists()
            .UseEmphasisExtras()
            .UseFooters()
            .UseCitations()
            .UseMathematics()
            .UseAutoIdentifiers()
            .Build();
        var html = Markdig.Markdown.ToHtml(Markdown, pipeline);

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
        _htmlContent = document.Body?.InnerHtml ?? "";
        await base.OnParametersSetAsync();
    }
}
