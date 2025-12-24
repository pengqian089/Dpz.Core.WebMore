using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Shared.Components;

public partial class SearchBox : ComponentBase
{
    [Parameter]
    public string? Keyword { get; set; }

    private bool _loading = false;
    
    protected override async Task OnInitializedAsync()
    {
        _loading = true;
        // 模拟一点延迟，否则太快了看不到骨架屏，或者如果有初始化逻辑可以放这里
        await Task.Delay(100); 
        _loading = false;
        await base.OnInitializedAsync();
    }

    private Task OnSearch()
    {
        if (string.IsNullOrWhiteSpace(Keyword))
        {
            return Task.CompletedTask;
        }
        
        // TODO search
        return Task.CompletedTask;
    }
}