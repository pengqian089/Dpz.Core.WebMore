using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Shared.Components;

public partial class SearchBox : ComponentBase
{
    [Parameter]
    public string? Keyword { get; set; }

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