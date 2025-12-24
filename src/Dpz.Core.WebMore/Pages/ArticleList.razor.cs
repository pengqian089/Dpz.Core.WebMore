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

    protected override async Task OnInitializedAsync()
    {
        PageIndex = PageIndex == 0 ? 1 : PageIndex;
        PageSize = PageSize == 0 ? 10 : PageSize;
        await base.OnInitializedAsync();
    }

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
        _source = await articleService.GetPageAsync(PageIndex, PageSize, Tag, "");
        _loading = false;
        PageIndex = _source.CurrentPageIndex;
        await base.OnParametersSetAsync();
    }
}
