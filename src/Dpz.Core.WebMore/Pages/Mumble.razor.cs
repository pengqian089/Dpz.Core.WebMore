using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Pages;

public partial class Mumble(IMumbleService mumbleService) : ComponentBase
{
    [Parameter]
    public int PageIndex { get; set; } = 1;

    [Parameter]
    public int PageSize { get; set; } = 10;

    private IPagedList<MumbleModel> _source = PagedList<MumbleModel>.Empty();
    private List<MumbleModel> _histories = [];

    private bool _loading = true;

    protected override async Task OnInitializedAsync()
    {
        if (_histories.Count == 0)
        {
            _histories = await mumbleService.GetHistories();
        }
        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        var pageIndex = PageIndex;
        if (pageIndex == 0)
        {
            pageIndex = 1;
        }
        _source = await mumbleService.GetPageAsync(pageIndex, PageSize, "");
        _loading = false;
        PageIndex = _source.CurrentPageIndex;
        await base.OnParametersSetAsync();
    }

    private async Task LikeAsync(string id)
    {
        var mumble = await mumbleService.LikeAsync(id);
        if (mumble != null)
        {
            var likeMumble = _source.FirstOrDefault(x => x.Id == id);
            likeMumble?.Like = mumble.Like;
            StateHasChanged();
        }
    }
}
