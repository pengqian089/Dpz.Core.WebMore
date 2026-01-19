using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Service.Impl;

public sealed class JsMusicPlayerService(IJSRuntime jsRuntime) : IMusicPlayerService
{
    private IJSObjectReference? _module;
    private IJSObjectReference? _player;
    private DotNetObjectReference<JsMusicPlayerService>? _objRef;

    public event Action<double>? TimeUpdated;
    public event Action<double>? DurationChanged;
    public event Action<bool>? PlayStateChanged;
    public event Action? Ended;
    public event Action<string>? Error;
    public event Action? NextRequested;
    public event Action? PrevRequested;

    public async Task InitializeAsync()
    {
        if (_player != null)
        {
            return;
        }

        _module = await jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            $"{Program.AssetsPrefix}/Shared/Components/MusicPlayer.razor.js"
        );
        _objRef = DotNetObjectReference.Create(this);
        _player = await _module.InvokeAsync<IJSObjectReference>("initAudioPlayer", _objRef);
    }

    public async Task SetSourceAsync(string url, string? trackId = null)
    {
        if (_player == null)
        {
            return;
        }

        await _player.InvokeVoidAsync("setSrc", url);
    }

    public async Task PlayAsync()
    {
        if (_player == null)
        {
            return;
        }

        await _player.InvokeVoidAsync("play");
    }

    public async Task PauseAsync()
    {
        if (_player == null)
        {
            return;
        }

        await _player.InvokeVoidAsync("pause");
    }

    public async Task SetCurrentTimeAsync(double time)
    {
        if (_player == null)
        {
            return;
        }

        await _player.InvokeVoidAsync("setCurrentTime", time);
    }

    public async Task UpdateMediaSessionAsync(string title, string artist, string coverUrl)
    {
        if (_player == null)
        {
            return;
        }

        await _player.InvokeVoidAsync("updateMediaSession", title, artist, coverUrl);
    }

    public async Task SaveStateAsync(MusicPlayerState state)
    {
        if (_player == null)
        {
            return;
        }

        await _player.InvokeVoidAsync("saveState", state);
    }

    public async Task<MusicPlayerState?> LoadStateAsync()
    {
        if (_player == null)
        {
            return null;
        }

        return await _player.InvokeAsync<MusicPlayerState?>("loadState");
    }

    [JSInvokable]
    public void OnTimeUpdate(double currentTime) => TimeUpdated?.Invoke(currentTime);

    [JSInvokable]
    public void OnEnded() => Ended?.Invoke();

    [JSInvokable]
    public void OnDurationChange(double duration) => DurationChanged?.Invoke(duration);

    [JSInvokable]
    public void OnPlayStateChange(bool isPlaying) => PlayStateChanged?.Invoke(isPlaying);

    [JSInvokable]
    public void OnError(string message) => Error?.Invoke(message);

    [JSInvokable]
    public void OnNext() => NextRequested?.Invoke();

    [JSInvokable]
    public void OnPrev() => PrevRequested?.Invoke();

    public async ValueTask DisposeAsync()
    {
        if (_player != null)
        {
            try
            {
                await _player.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
            catch (JSException)
            {
            }
        }

        if (_module != null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
            }
            catch (JSException)
            {
            }
        }

        _objRef?.Dispose();
    }
}