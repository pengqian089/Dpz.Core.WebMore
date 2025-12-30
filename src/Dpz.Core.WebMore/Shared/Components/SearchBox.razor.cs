using System.Net;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Dpz.Core.WebMore.Shared.Components;

public partial class SearchBox(NavigationManager navigationManager, IAppDialogService dialogService)
    : ComponentBase
{
    [Parameter]
    public string? Keyword { get; set; }

    private string? _keyword;

    protected override Task OnParametersSetAsync()
    {
        _keyword = Keyword;
        return base.OnParametersSetAsync();
    }

    private async Task OnSearch()
    {
        if (string.IsNullOrWhiteSpace(_keyword))
        {
            await dialogService.AlertAsync("请输入关键字");
            return;
        }

        navigationManager.NavigateTo($"/article/search?keyword={WebUtility.UrlEncode(_keyword)}");
    }

    private async Task HandleKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            await OnSearch();
        }
    }
}
