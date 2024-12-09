using System;
using System.Linq;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages
{
    public partial class Mumble
    {
        [Parameter] public int PageIndex { get; set; } = 1;
        [Parameter] public int PageSize { get; set; } = 10;
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private IMumbleService MumbleService { get; set; }
        
        [Inject] private IJSRuntime JsRuntime { get; set; }

        private IPagedList<MumbleModel> _source = new PagedList<MumbleModel>(Array.Empty<MumbleModel>(), 0, 0);

        private bool _loading = true;

        protected override async Task OnInitializedAsync()
        {
            PageIndex = PageIndex == 0 ? 1 : PageIndex;
            PageSize = PageSize == 0 ? 10 : PageSize;
            await base.OnInitializedAsync();
        }

        protected override async Task OnParametersSetAsync()
        {
            _loading = true;
            PageIndex = PageIndex == 0 ? 1 : PageIndex;
            _source = await MumbleService.GetPageAsync(PageIndex, PageSize, "");
            _loading = false;
            PageIndex = _source.CurrentPageIndex;
            await base.OnParametersSetAsync();
        }

        private void ToPageAsync(int page)
        {
            PageIndex = page;
            Navigation.NavigateTo(page == 1 ? "/mumble" : $"/mumble/{page}");
        }
        
        private async Task LikeAsync(string id)
        {
            var mumble = await MumbleService.LikeAsync(id);
            var likeMumble = _source.FirstOrDefault(x => x.Id == id);
            if (likeMumble != null)
                likeMumble.Like = mumble.Like;
            StateHasChanged();
        }
    }
}