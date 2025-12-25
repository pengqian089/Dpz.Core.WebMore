using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Pages;

public partial class SteamList(ISteamService steamService) : ComponentBase
{
    private List<SteamModel> _steamLibraries = [];
    private bool _isLoading = true;
    private bool _animate;

    protected override async Task OnInitializedAsync()
    {
        if (_steamLibraries.Count == 0)
        {
            _isLoading = true;
            _steamLibraries = await steamService.GetSteamLibrariesAsync();
            _isLoading = false;
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isLoading && _steamLibraries.Count > 0 && !_animate)
        {
            // 给一点延迟确保渲染完成后再执行动画
            await Task.Delay(100);
            _animate = true;
            StateHasChanged();
        }
        await base.OnAfterRenderAsync(firstRender);
    }
}
