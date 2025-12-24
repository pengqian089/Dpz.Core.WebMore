using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Shared.Components;

public partial class ArticleNews(IArticleService articleService) : ComponentBase
{
    private List<ArticleMiniModel> _source = [];

    protected override async Task OnInitializedAsync()
    {
        if (_source.Count == 0)
        {
            _source = await articleService.GetNewsAsync();
        }
        await base.OnInitializedAsync();
    }
}
