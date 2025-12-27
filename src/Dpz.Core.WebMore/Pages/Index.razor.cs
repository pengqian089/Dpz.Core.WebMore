using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages;

public partial class Index(
    IArticleService articleService,
    ICommunityService communityService,
    IVideoService videoService,
    IJSRuntime jsRuntime
) : ComponentBase, IDisposable, IAsyncDisposable
{
    private List<ArticleMiniModel> _topArticles = [];
    private List<PictureRecordModel> _banners = [];
    private bool _loading = true;

    // Banner Rotation
    private int _currentBannerIndex;
    private Timer? _bannerTimer;

    // Video
    private VideoModel? _currentVideo;
    private bool _videoInitialized;
    private IJSObjectReference? _videoModule;

    protected override async Task OnInitializedAsync()
    {
        var tasks = new List<Task>
        {
            Task.Run(async () => _topArticles = await articleService.GetViewTopAsync()),
            Task.Run(async () => _banners = await communityService.GetBannersAsync()),
            Task.Run(async () =>
            {
                var videos = await videoService.GetVideosAsync();
                if (videos.Count > 0)
                {
                    _currentVideo = videos[Random.Shared.Next(videos.Count)];
                }
            }),
        };

        await Task.WhenAll(tasks);
        _loading = false;

        StartBannerRotation();

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_loading && !_videoInitialized && _currentVideo != null)
        {
            _videoInitialized = true;
            await Task.Delay(100);

            try
            {
                _videoModule = await jsRuntime.InvokeAsync<IJSObjectReference>(
                    "import",
                    "./Pages/Index.razor.js"
                );
                await _videoModule.InvokeVoidAsync("initVideo", _currentVideo, Program.BaseAddress);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize video player: {ex.Message}");
            }
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private void StartBannerRotation()
    {
        if (_banners.Count <= 1)
        {
            return;
        }

        _bannerTimer = new Timer(
            _ =>
            {
                _currentBannerIndex = (_currentBannerIndex + 1) % _banners.Count;
                InvokeAsync(StateHasChanged);
            },
            null,
            5000,
            5000
        );
    }

    public void Dispose()
    {
        _bannerTimer?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (_videoModule != null)
        {
            try
            {
                await _videoModule.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Ignore if JS side is already disconnected
            }
        }
    }
}
