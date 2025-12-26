using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages;

public partial class Albums(IPictureRecordService pictureRecordService, IJSRuntime jsRuntime)
    : ComponentBase,
        IAsyncDisposable
{
    private readonly List<PictureRecordModel> _pictures = [];
    private int _pageIndex = 1;
    private const int PageSize = 20;
    private bool _hasMore = true;
    private bool _isLoading;
    private int _totalItemCount;
    private int _totalPageCount;

    private IJSObjectReference? _module;
    private IJSObjectReference? _jsAlbums;
    private DotNetObjectReference<Albums>? _objRef;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./Pages/Albums.razor.js"
            );
            _objRef = DotNetObjectReference.Create(this);
            _jsAlbums = await _module.InvokeAsync<IJSObjectReference>("create", _objRef, ".albums");

            if (_pictures.Count > 0)
            {
                await _jsAlbums.InvokeVoidAsync("init", _pictures);
            }
        }
    }

    private async Task LoadDataAsync()
    {
        if (_isLoading || !_hasMore)
        {
            return;
        }

        _isLoading = true;
        StateHasChanged();

        try
        {
            var page = await pictureRecordService.GetPagesAsync(
                tags: null,
                description: null,
                pageIndex: _pageIndex,
                pageSize: PageSize
            );

            _totalItemCount = page.TotalItemCount;
            _totalPageCount = page.TotalPageCount;

            if (page.Count > 0)
            {
                _pictures.AddRange(page);

                // If we already have the JS module loaded (loaded subsequent pages), append items
                if (_jsAlbums != null)
                {
                    await _jsAlbums.InvokeVoidAsync("appendItems", page);
                }

                _pageIndex++;
                _hasMore = page.CurrentPageIndex < page.TotalPageCount;
            }
            else
            {
                _hasMore = false;
            }
        }
        finally
        {
            _isLoading = false;
            StateHasChanged();
        }
    }

    [JSInvokable]
    public async Task LoadMore()
    {
        await LoadDataAsync();
    }

    private async Task OpenGallery(int index)
    {
        if (_jsAlbums != null)
        {
            await _jsAlbums.InvokeVoidAsync("openGallery", index);
        }
    }

    private async Task DownloadImage(string url)
    {
        if (_jsAlbums != null)
        {
            await _jsAlbums.InvokeVoidAsync("downloadImage", url);
        }
    }

    private async Task ShareImage(string url)
    {
        if (_jsAlbums != null)
        {
            await _jsAlbums.InvokeVoidAsync("shareImage", url);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsAlbums != null)
        {
            await _jsAlbums.InvokeVoidAsync("dispose");
            await _jsAlbums.DisposeAsync();
        }
        if (_module != null)
        {
            await _module.DisposeAsync();
        }
        _objRef?.Dispose();
    }
}
