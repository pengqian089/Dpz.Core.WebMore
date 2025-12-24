using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Pages;

public partial class SteamList(ISteamService steamService)
{
    private List<SteamModel> _steamLibraries = [];
    private bool _isLoading = true;

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
}
