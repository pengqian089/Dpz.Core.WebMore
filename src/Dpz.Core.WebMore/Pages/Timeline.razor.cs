using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages;

public partial class Timeline(ITimelineService timelineService, IJSRuntime jsRuntime)
    : ComponentBase,
        IAsyncDisposable
{
    private List<TimelineItemViewModel> _source = [];
    private bool _isLoading = true;
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _observer;
    private DotNetObjectReference<Timeline>? _dotNetRef;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            var data = await timelineService.GetTimelineAsync();
            _source = data.Select(x => new TimelineItemViewModel { Model = x }).ToList();
        }
        finally
        {
            _isLoading = false;
        }
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_source.Count > 0 && _observer == null)
        {
            try
            {
                _jsModule = await jsRuntime.InvokeAsync<IJSObjectReference>(
                    "import",
                    "./Pages/Timeline.razor.js"
                );

                _dotNetRef = DotNetObjectReference.Create(this);

                // Initialize observer only when we have items
                _observer = await _jsModule.InvokeAsync<IJSObjectReference>(
                    "initTimeline",
                    _dotNetRef,
                    ".timeline__item"
                );
            }
            catch (JSException)
            {
                // Handle JS loading errors if necessary
            }
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    [JSInvokable]
    public void OnItemsVisible(int[] indices)
    {
        var hasChanges = false;
        foreach (var index in indices)
        {
            if (index >= 0 && index < _source.Count)
            {
                var item = _source[index];
                if (!item.IsVisible)
                {
                    item.IsVisible = true;
                    hasChanges = true;
                }
            }
        }

        if (hasChanges)
        {
            StateHasChanged();
        }
    }

    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_observer != null)
            {
                await _observer.InvokeVoidAsync("dispose");
                await _observer.DisposeAsync();
            }

            if (_jsModule != null)
            {
                await _jsModule.DisposeAsync();
            }

            _dotNetRef?.Dispose();
        }
        catch (JSDisconnectedException)
        {
            // Ignore if JS is already disconnected
        }
    }

    public class TimelineItemViewModel
    {
        public required TimelineModel Model { get; set; }
        public bool IsVisible { get; set; }
    }
}
