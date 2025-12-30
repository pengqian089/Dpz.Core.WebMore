using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Pages;

public partial class ArticleSearch(IArticleService articleService) : ComponentBase
{
    [SupplyParameterFromQuery(Name = "keyword")]
    public string? Keyword { get; set; }

    private List<ArticleSearchResultModel> _searchResult = [];

    private bool _loading = false;

    protected override async Task OnParametersSetAsync()
    {
        if (string.IsNullOrWhiteSpace(Keyword))
        {
            _searchResult.Clear();
            return;
        }

        _loading = true;
        _searchResult.Clear();
        StateHasChanged();

        try
        {
            _searchResult = await articleService.SearchAsync(Keyword);
        }
        finally
        {
            _loading = false;
        }
    }
}
