using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Dpz.Core.WebMore.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages;

public partial class Mumble(
    IMumbleService mumbleService,
    IJSRuntime jsRuntime,
    IAppDialogService appDialogService
) : ComponentBase, IAsyncDisposable
{
    [Parameter]
    public int PageIndex { get; set; } = 1;

    [Parameter]
    public int PageSize { get; set; } = 10;

    private IPagedList<MumbleModel> _source = PagedList<MumbleModel>.Empty();
    private List<MumbleViewModel> _viewModels = [];
    private List<MumbleModel> _histories = [];

    private bool _loading = true;
    private IJSObjectReference? _module;
    private IJSObjectReference? _observer;
    private DotNetObjectReference<Mumble>? _objRef;

    protected override async Task OnInitializedAsync()
    {
        if (_histories.Count == 0)
        {
            _histories = await mumbleService.GetHistories();
        }
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _objRef = DotNetObjectReference.Create(this);
            try
            {
                _module = await jsRuntime.InvokeAsync<IJSObjectReference>(
                    "import",
                    "./Pages/Mumble.razor.js"
                );
                // Create instance via factory function
                _observer = await _module.InvokeAsync<IJSObjectReference>("createObserver");
                await _observer.InvokeVoidAsync("init", _objRef);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Mumble] Failed to init JS: {ex}");
            }
        }

        // Register elements for observation
        await EnsureObservation();
    }

    private async Task EnsureObservation()
    {
        if (_observer is not null && !_loading)
        {
            foreach (var vm in _viewModels.Where(x => !x.IsObserved))
            {
                if (vm.ContentRef.Context != null)
                {
                    try
                    {
                        await _observer.InvokeVoidAsync("observe", vm.ContentRef, vm.Model.Id);
                        vm.IsObserved = true;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Mumble] Failed to observe {vm.Model.Id}: {ex}");
                    }
                }
            }
        }
    }

    [JSInvokable]
    public void UpdateExpandState(string id, bool needExpand)
    {
        var vm = _viewModels.FirstOrDefault(x => x.Model.Id == id);
        if (vm != null && vm.NeedExpand != needExpand)
        {
            vm.NeedExpand = needExpand;
            StateHasChanged();
        }
    }

    protected override async Task OnParametersSetAsync()
    {
        _loading = true;
        var pageIndex = PageIndex;
        if (pageIndex == 0)
        {
            pageIndex = 1;
        }
        _source = await mumbleService.GetPageAsync(pageIndex, PageSize, "");

        // Map to ViewModel
        _viewModels = _source
            .Select(x => new MumbleViewModel { Model = x, IsExpanded = false })
            .ToList();

        // Check likes from local storage
        foreach (var vm in _viewModels)
        {
            var isLiked = await jsRuntime.InvokeAsync<string>(
                "localStorage.getItem",
                $"mumble_like_{vm.Model.Id}"
            );
            vm.IsLiked = !string.IsNullOrEmpty(isLiked);
        }

        _loading = false;
        PageIndex = _source.CurrentPageIndex;
        await base.OnParametersSetAsync();
    }

    private async Task LikeAsync(MumbleViewModel vm)
    {
        if (vm.IsLiked)
        {
            appDialogService.Toast("您已经点赞过了");
            return;
        }

        vm.IsLiked = true; // Optimistic update
        var mumble = await mumbleService.LikeAsync(vm.Model.Id);

        if (mumble != null)
        {
            vm.Model.Like = mumble.Like;
            await jsRuntime.InvokeVoidAsync(
                "localStorage.setItem",
                $"mumble_like_{vm.Model.Id}",
                "true"
            );
        }
        else
        {
            vm.IsLiked = false;
            appDialogService.Toast("点赞失败，请稍后重试", ToastType.Error);
        }
        StateHasChanged();
    }

    private async Task OnDetail(MumbleViewModel vm)
    {
        await appDialogService.ShowComponentAsync(
            "碎碎念详情",
            builder =>
            {
                builder.OpenComponent<MarkdownPreview>(0);
                builder.AddAttribute(1, "Markdown", vm.Model.Markdown);
                builder.CloseComponent();
            },
            "800px" // Default width
        );
    }

    public async ValueTask DisposeAsync()
    {
        _objRef?.Dispose();

        if (_observer is not null)
        {
            try
            {
                await _observer.InvokeVoidAsync("dispose");
                await _observer.DisposeAsync();
            }
            catch
            {
                // ignored
            }
        }

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

    public class MumbleViewModel
    {
        public required MumbleModel Model { get; set; }
        public bool IsLiked { get; set; }
        public bool IsExpanded { get; set; }
        public bool ShowComments { get; set; }

        // Use ElementReference to get actual DOM element
        public ElementReference ContentRef { get; set; }

        public bool NeedExpand { get; set; }
        public bool IsObserved { get; set; }
    }
}
