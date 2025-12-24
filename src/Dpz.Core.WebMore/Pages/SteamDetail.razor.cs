using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Pages;

public partial class SteamDetail(ISteamService steamService)
{
    [Parameter]
    [EditorRequired]
    public required int Id { get; set; }

    private SteamModel? _model;
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        if (_model == null)
        {
            _isLoading = true;
            _model = await steamService.GetLibraryAsync(Id);
            _isLoading = false;
        }

        await base.OnInitializedAsync();
    }
}
