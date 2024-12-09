using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages
{
    public partial class Index
    {
        [Inject] private IJSRuntime JsRuntime { get; set; }

        [Inject] private IArticleService ArticleService { get; set; }
        
        [Inject]private ICommunityService CommunityService { get; set; }
        
        [Inject]private IConfiguration Configuration { get; set; }

        private List<ArticleMiniModel> _topArticles = new();

        private List<PictureModel> _banners = new();

        private bool _loading = true;

        protected override async Task OnInitializedAsync()
        {
            _topArticles = await ArticleService.GetViewTopAsync();
            _banners = await CommunityService.GetBannersAsync();
            _loading = false;
            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if(!_loading)
                await JsRuntime.InvokeVoidAsync("initIndex");
            await base.OnAfterRenderAsync(firstRender);
        }
    }
}