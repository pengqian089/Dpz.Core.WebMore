using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages;

public partial class Article(IArticleService articleService, IJSRuntime jsRuntime)
{
    [Parameter]
    [EditorRequired]
    public required string Id { get; set; }

    [Parameter]
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
        _article = article;
        _loading = false;
        await base.OnParametersSetAsync();
    }
}
