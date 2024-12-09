using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages;

public partial class Timeline
{
    private bool _isLoading = false;

    private double _width = 0d;

    private DotNetObjectReference<Timeline> _objectReference;

    [DynamicDependency(nameof(UpdateWindowWidth))]
    public Timeline()
    {
        
    }

    [Inject] private IJSRuntime JsRuntime { get; set; }

    [Inject]private ITimelineService TimelineService { get; set; }

    private List<TimelineModel> _source = new();

    protected override async Task OnInitializedAsync()
    {
        _isLoading = true;
        _objectReference = DotNetObjectReference.Create(this);
        _width = await JsRuntime.InvokeAsync<double>("getWindowWidth");
        _source = await TimelineService.GetTimelineAsync();
        _isLoading = false;
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            await InitWindowWidthListener();
        }
    }

    private async Task InitWindowWidthListener()
    {
        await JsRuntime.InvokeVoidAsync("addWindowWidthListener", _objectReference);
    }

    public async ValueTask DisposeAsync()
    {
        await JsRuntime.InvokeVoidAsync("removeWindowWidthListener", _objectReference);
        _objectReference?.Dispose();
    }

    [JSInvokable]
    public Task UpdateWindowWidth(int windowWidth)
    {
        _width = windowWidth;
        StateHasChanged();
        return Task.CompletedTask;
    }
}