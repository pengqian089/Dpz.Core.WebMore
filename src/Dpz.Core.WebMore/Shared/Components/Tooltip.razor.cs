using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Shared.Components;

public partial class Tooltip(IJSRuntime jsRuntime) : ComponentBase, IAsyncDisposable
{
    [Parameter]
    [EditorRequired]
    public required RenderFragment ChildContent { get; set; }

    [Parameter]
    public string? Text { get; set; }

    [Parameter]
    public TooltipPlacement? Placement { get; set; }

    [Parameter]
    public string? Class { get; set; }

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = [];

    private ElementReference _triggerElement;
    private ElementReference _contentElement;
    private IJSObjectReference? _module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./Shared/Components/Tooltip.razor.js"
            );
        }
    }

    private async Task Show()
    {
        if (_module is not null && !string.IsNullOrEmpty(Text))
        {
            await _module.InvokeVoidAsync(
                "show",
                _triggerElement,
                _contentElement,
                Placement?.ToString()
            );
        }
    }

    private async Task Hide()
    {
        if (_module is not null)
        {
            await _module.InvokeVoidAsync("hide", _contentElement);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch
            {
                // ignored
            }
        }
    }
}

public enum TooltipPlacement
{
    Top,
    Bottom,
    Left,
    Right,
}
