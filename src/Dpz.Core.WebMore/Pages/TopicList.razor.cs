using System;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages
{
    public partial class TopicList
    {
        [Parameter] public int PageIndex { get; set; } = 1;
        [Parameter] public int PageSize { get; set; } = 10;
        [Inject] private ITopicService TopicService { get; set; }
        [Inject] private IJSRuntime JsRuntime { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }

        private IPagedList<TopicModel> _source = new PagedList<TopicModel>(Array.Empty<TopicModel>(), 0, 10);

        private bool _loading = true;
        
        protected override async Task OnInitializedAsync()
        {
            PageIndex = PageIndex == 0 ? 1 : PageIndex;
            PageSize = PageSize == 0 ? 10 : PageSize;
            await base.OnInitializedAsync();
        }
        
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JsRuntime.InvokeVoidAsync("showLazyImage");
            if (!firstRender)
                await JsRuntime.InvokeVoidAsync("articleInit");
            await base.OnAfterRenderAsync(firstRender);
        }
        
        protected override async Task OnParametersSetAsync()
        {
            _loading = true;
            _source = await TopicService.GetTopicPageAsync(PageIndex, PageSize);
            _loading = false;
            PageIndex = _source.CurrentPageIndex;
            await base.OnParametersSetAsync();
        }
        
        private void ToPageAsync(int page)
        {
            PageIndex = page;
            Navigation.NavigateTo(page == 1 ? "/topic/list" : $"/topic/list/{page}");
        }
    }
}