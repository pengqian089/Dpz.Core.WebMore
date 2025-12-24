using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Shared.Components;

public partial class TagCloud(IArticleService articleService) : ComponentBase
{
    [Parameter]
    public string? Tag { get; set; }
    
    private string[] _tags = [];

    protected override async Task OnInitializedAsync()
    {
        if (_tags.Length == 0)
        {
            _tags = await articleService.GetTagsAsync();
        }
        await base.OnInitializedAsync();
    }
}
