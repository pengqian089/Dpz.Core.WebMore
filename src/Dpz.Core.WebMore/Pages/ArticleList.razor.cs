using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Pages;

public partial class ArticleList(IArticleService articleService, NavigationManager navigation)
{
    [Parameter]
    public int PageIndex { get; set; } = 1;

    [Parameter]
    public int PageSize { get; set; } = 10;

    [Parameter]
    public string? Tag { get; set; }

    private IPagedList<ArticleMiniModel> _source = new PagedList<ArticleMiniModel>([], 0, 10, 0);

    private bool _loading = true;

    private string LinkTemplate()
    {
        if (string.IsNullOrWhiteSpace(Tag))
        {
            return "/article/list/{0}";
        }
        return "/article/list/Tag/{Tag}/{0}";
    }

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        var pageIndex = PageIndex;
        if (pageIndex == 0)
        {
            pageIndex = 1;
        }
        var pageSize = PageSize;
        if (pageSize == 0)
        {
            pageSize = 10;
        }

        _source = await articleService.GetPageAsync(pageIndex, pageSize, Tag, "");
        _loading = false;
        PageIndex = _source.CurrentPageIndex;
        await base.OnParametersSetAsync();
    }
}
