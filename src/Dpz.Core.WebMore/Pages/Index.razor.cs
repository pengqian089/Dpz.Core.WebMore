using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;

namespace Dpz.Core.WebMore.Pages;

public partial class Index(IArticleService articleService, ICommunityService communityService)
{
    private List<ArticleMiniModel> _topArticles = [];

    private List<PictureRecordModel> _banners = [];

    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        _topArticles = await articleService.GetViewTopAsync();
        _banners = await communityService.GetBannersAsync();
        _loading = false;
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_loading)
        {
            //await JsRuntime.InvokeVoidAsync("initIndex");
        }
        await base.OnAfterRenderAsync(firstRender);
    }
}
