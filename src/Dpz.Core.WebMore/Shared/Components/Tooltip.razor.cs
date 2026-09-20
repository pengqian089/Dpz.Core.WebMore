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

    /// <summary>
    /// 是否阻止右键菜单/触摸长按菜单，默认为 true
    /// </summary>
    [Parameter]
    public bool PreventContextMenu { get; set; } = true;

    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object> AdditionalAttributes { get; set; } = [];

    private bool _preventContextMenu => PreventContextMenu;

    private void OnContextMenu()
    {
        // 事件处理由 @oncontextmenu:preventDefault 控制
    }

    private ElementReference _triggerElement;
    private ElementReference _contentElement;
    private IJSObjectReference? _module;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _module = await jsRuntime.InvokeAsync<IJSObjectReference>(
                    "import",
                    "./Shared/Components/Tooltip.razor.js"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tooltip 加载 JS 模块失败：{ex.Message}");
            }
        }
    }

    private async Task Show()
    {
        if (_module is not null && !string.IsNullOrEmpty(Text))
        {
            try
            {
                await _module.InvokeVoidAsync(
                    "show",
                    _triggerElement,
                    _contentElement,
                    Placement?.ToString()
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tooltip 显示失败：{ex.Message}");
            }
        }
    }

    private async Task Hide()
    {
        if (_module is not null)
        {
            try
            {
                await _module.InvokeVoidAsync("hide", _contentElement);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tooltip 隐藏失败：{ex.Message}");
            }
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
