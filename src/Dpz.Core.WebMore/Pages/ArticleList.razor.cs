using System;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;

namespace Dpz.Core.WebMore.Pages
{
    public partial class ArticleList
    {
        [Parameter] public int PageIndex { get; set; } = 1;
        [Parameter] public int PageSize { get; set; } = 10;
        [Parameter]public string Tag { get; set; }
        [Inject] private IArticleService ArticleService { get; set; }
        [Inject] private IJSRuntime JsRuntime { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }

        private MudPagination _mudPagination;

        private IPagedList<ArticleMiniModel> _source =
            new PagedList<ArticleMiniModel>(Array.Empty<ArticleMiniModel>(), 0, 10, 0);

        private bool _loading = true;
        
        protected override async Task OnInitializedAsync()
        {
            PageIndex = PageIndex == 0 ? 1 : PageIndex;
            PageSize = PageSize == 0 ? 10 : PageSize;
            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender)
                await JsRuntime.InvokeVoidAsync("articleInit");
            await base.OnAfterRenderAsync(firstRender);
        }

        private void ToPageAsync(int page)
        {
            PageIndex = page;
            var tagPath = string.IsNullOrEmpty(Tag) ? "" : $"/Tag/{Tag}";
            var path = $"/article/list{tagPath}";
            if (page == 1)
            {
                Navigation.NavigateTo((path));   
            }
            else
            {
                Navigation.NavigateTo($"{path}/" + PageIndex);
            }
        }

        protected override async Task OnParametersSetAsync()
        {
            _loading = true;
            _source = await ArticleService.GetPageAsync(PageIndex, PageSize, Tag, "");
            _loading = false;
            PageIndex = _source.CurrentPageIndex;
            //_mudPagination.
            await base.OnParametersSetAsync();
        }
    }
}