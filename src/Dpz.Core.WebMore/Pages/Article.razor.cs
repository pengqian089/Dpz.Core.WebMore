using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages;

public partial class Article(IArticleService articleService)
{
    [Parameter]
    [EditorRequired]
    public required string Id { get; set; }

    [SupplyParameterFromQuery(Name = "text")]
    public string? Text { get; set; }

    private ArticleModel? _article;

    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        var article = await articleService.GetArticleAsync(Id);
        if (article == null)
        {
            // TODO show error
            _loading = false;
            return;
        }

        // Apply highlighting if search text is present
        if (!string.IsNullOrEmpty(Text))
        {
            article.Markdown = HighlightHelper.HighlightKeywords(article.Markdown, Text);

            if (!string.IsNullOrEmpty(article.Introduction))
            {
                article.Introduction = HighlightHelper.HighlightKeywords(
                    article.Introduction,
                    Text
                );
            }

            article.Title = HighlightHelper.HighlightKeywords(article.Title, Text);
        }

        _article = article;
        _loading = false;
        await base.OnParametersSetAsync();
    }
}
