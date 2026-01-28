using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Shared.Components;

public partial class MusicPlayer(
    IMusicService musicService,
    IMusicPlayerService musicPlayerService,
    IJSRuntime jsRuntime,
    ILogger<MusicPlayer> logger
) : ComponentBase, IAsyncDisposable
{
    private IJSObjectReference? _uiModule;
    private IJSObjectReference? _uiController;
    private DotNetObjectReference<MusicPlayer>? _uiRef;
    private Timer? _progressSaveTimer;

    private List<MusicModel> _musics = [];
    private MusicModel? _currentTrack;
    private int _currentIndex = -1;
    private bool _isPlaying;
    private double _currentTime;
    private double _duration;
    private PlayMode _playMode = PlayMode.Order;
    private bool _showLyrics;
    private bool _showList;
    private bool _lyricsOnBackground;

    private readonly List<LyricLine> _lyrics = [];
    private int _currentLyricIndex = -1;

    // 迷你播放器进度环状态
    private const double Circumference = 213.6;
    private double _progressOffset = 213.6;

    private enum PlayMode
    {
        Order,
        Random,
        Single,
    }

    private record LyricLine(double Time, string Text);

    private Task<List<MusicModel>>? _musicLoadTask;

    protected override async Task OnInitializedAsync()
    {
        musicPlayerService.TimeUpdated += HandleTimeUpdate;
        musicPlayerService.DurationChanged += HandleDurationChange;
        musicPlayerService.PlayStateChanged += HandlePlayStateChange;
        musicPlayerService.Ended += HandleEnded;
        musicPlayerService.Error += HandleError;
        musicPlayerService.NextRequested += HandleNextRequested;
        musicPlayerService.PrevRequested += HandlePrevRequested;

        // 启动音乐列表加载（只加载一次）
        _musicLoadTask ??= LoadMusicsAsync();
        await _musicLoadTask;
    }

    /// <summary>
    /// 加载音乐列表（确保只执行一次）
    /// </summary>
    private async Task<List<MusicModel>> LoadMusicsAsync()
    {
        if (_musics.Count == 0)
        {
            var musics = await musicService.GetMusicPageAsync(1, 1000);
            _musics = musics.ToList();
        }
        return _musics;
    }

    /// <summary>
    /// 渲染后初始化 JS 模块和播放器
    /// </summary>
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // 导入 UI JS 模块
            _uiModule = await jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                $"{Program.AssetsPrefix}/Shared/Components/MusicPlayer.razor.js"
            );
            _uiRef = DotNetObjectReference.Create(this);
            _uiController = await _uiModule.InvokeAsync<IJSObjectReference>(
                "initMusicPlayerUi",
                _uiRef
            );

            await musicPlayerService.InitializeAsync();

            // 确保音乐列表加载完成
            if (_musicLoadTask != null)
            {
                await _musicLoadTask;
            }

            // 恢复保存的状态，或加载第一首歌
            await RestoreStateAsync();
        }
    }

    /// <summary>
    /// 从 localStorage 恢复状态
    /// </summary>
    private async Task RestoreStateAsync()
    {
        if (_musics.Count == 0)
        {
            Console.WriteLine("RestoreStateAsync: Player or music list not ready");
            return;
        }

        var trackIndexToLoad = 0;
        var startTime = 0d;

        try
        {
            var state = await musicPlayerService.LoadStateAsync();

            if (state != null)
            {
                // 恢复播放模式
                if (Enum.TryParse<PlayMode>(state.PlayModeStr, out var mode))
                {
                    _playMode = mode;
                }

                // 恢复歌词显示状态
                _showLyrics = state.ShowLyrics;
                _lyricsOnBackground = state.LyricsOnBackground;

                // 查找保存的歌曲
                if (!string.IsNullOrEmpty(state.TrackId))
                {
                    var trackIndex = _musics.FindIndex(m => m.Id == state.TrackId);
                    if (trackIndex >= 0)
                    {
                        trackIndexToLoad = trackIndex;
                        startTime = state.CurrentTime;
                        Console.WriteLine($"Restored state: Track {trackIndex}, Time {startTime}");
                    }
                    else
                    {
                        Console.WriteLine($"Track with ID {state.TrackId} not found in playlist");
                    }
                }
            }

            // 加载歌曲（有记录则加载记录的歌曲，否则加载第一首）
            await LoadTrack(trackIndexToLoad, false);

            // 如果有保存的播放进度，设置播放位置
            if (startTime > 0)
            {
                // 延迟一下确保音频源已加载
                await Task.Delay(100);
                await musicPlayerService.SetCurrentTimeAsync(startTime);
            }

            StateHasChanged();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "恢复播放状态失败");
            // 失败时至少加载第一首歌
            if (_currentIndex < 0 && _musics.Count > 0)
            {
                await LoadTrack(0, false);
            }
        }
    }

    private async Task SaveStateAsync()
    {
        await musicPlayerService.SaveStateAsync(
            new MusicPlayerState(
                _playMode.ToString(),
                _showLyrics,
                _lyricsOnBackground,
                _currentTrack?.Id,
                _currentTime
            )
        );
    }

    /// <summary>
    /// JS 调用：保存播放进度
    /// </summary>
    [JSInvokable]
    public async Task SavePlayProgress(string trackId, double currentTime)
    {
        await musicPlayerService.SaveStateAsync(
            new MusicPlayerState(
                _playMode.ToString(),
                _showLyrics,
                _lyricsOnBackground,
                trackId,
                currentTime
            )
        );
    }

    /// <summary>
    /// 加载指定索引的歌曲
    /// </summary>
    /// <param name="index">歌曲索引</param>
    /// <param name="autoPlay">是否自动播放</param>
    private async Task LoadTrack(int index, bool autoPlay = true)
    {
        if (index < 0 || index >= _musics.Count)
        {
            return;
        }

        _currentIndex = index;
        _currentTrack = _musics[_currentIndex];
        ParseLyrics(_currentTrack.LyricContent);

        await musicPlayerService.SetSourceAsync(_currentTrack.MusicUrl, _currentTrack.Id);
        await UpdateMediaSession();

        if (_showList && _uiController != null)
        {
            await _uiController.InvokeVoidAsync(
                "scrollToItem",
                $"track-item-{_currentIndex}",
                "nearest"
            );
        }

        if (autoPlay)
        {
            await Play();
        }
        else
        {
            _isPlaying = false;
            StateHasChanged();
        }
    }

    /// <summary>
    /// 更新系统媒体会话
    /// </summary>
    private async Task UpdateMediaSession()
    {
        if (_currentTrack != null)
        {
            await musicPlayerService.UpdateMediaSessionAsync(
                _currentTrack.Title ?? string.Empty,
                _currentTrack.Artist ?? string.Empty,
                _currentTrack.CoverUrl ?? string.Empty
            );
        }
    }

    /// <summary>
    /// 解析歌词内容
    /// </summary>
    private void ParseLyrics(string? lrcContent)
    {
        _lyrics.Clear();
        _currentLyricIndex = -1;

        if (string.IsNullOrEmpty(lrcContent))
        {
            return;
        }

        var lines = lrcContent.Split('\n');
        // 正则匹配歌词时间标签 [mm:ss.xx]
        var regex = LyricRegex();

        foreach (var line in lines)
        {
            var match = regex.Match(line);
            if (!match.Success)
            {
                continue;
            }

            var min = int.Parse(match.Groups[1].Value);
            var sec = int.Parse(match.Groups[2].Value);
            var msStr = match.Groups[3].Value;
            // 处理 2位或3位 毫秒数
            var ms = msStr.Length == 3 ? int.Parse(msStr) : int.Parse(msStr) * 10;
            var time = min * 60 + sec + ms / 1000.0;
            var text = match.Groups[4].Value.Trim();

            if (!string.IsNullOrEmpty(text))
            {
                _lyrics.Add(new LyricLine(time, text));
            }
        }
    }

    /// <summary>
    /// 切换播放/暂停状态
    /// </summary>
    private async Task TogglePlay()
    {
        if (_isPlaying)
        {
            await musicPlayerService.PauseAsync();
        }
        else
        {
            await musicPlayerService.PlayAsync();
        }
    }

    private async Task Play()
    {
        await musicPlayerService.PlayAsync();
    }

    private async Task Pause()
    {
        await musicPlayerService.PauseAsync();
    }

    private async Task Next()
    {
        await Skip(1);
    }

    private async Task Prev()
    {
        await Skip(-1);
    }

    /// <summary>
    /// 跳过歌曲
    /// </summary>
    /// <param name="direction">方向：1 为下一首，-1 为上一首</param>
    private async Task Skip(int direction)
    {
        if (_musics.Count == 0)
        {
            return;
        }

        int nextIndex;
        if (_playMode == PlayMode.Random)
        {
            if (_musics.Count > 1)
            {
                var rnd = new Random();
                do
                {
                    nextIndex = rnd.Next(_musics.Count);
                } while (nextIndex == _currentIndex);
            }
            else
            {
                nextIndex = 0;
            }
        }
        else
        {
            nextIndex = (_currentIndex + direction + _musics.Count) % _musics.Count;
        }

        await LoadTrack(nextIndex);
    }

    /// <summary>
    /// 进度条拖动事件
    /// </summary>
    private async Task OnSeekChange(ChangeEventArgs e)
    {
        if (double.TryParse(e.Value?.ToString(), out var pct))
        {
            var time = _duration * (pct / 100.0);
            await musicPlayerService.SetCurrentTimeAsync(time);
        }
    }

    /// <summary>
    /// 切换播放模式
    /// </summary>
    private async Task CycleModeAsync()
    {
        _playMode = _playMode switch
        {
            PlayMode.Order => PlayMode.Random,
            PlayMode.Random => PlayMode.Single,
            _ => PlayMode.Order,
        };

        // 保存状态
        await SaveStateAsync();
    }

    /// <summary>
    /// 切换歌词显示状态（关闭 -> 面板 -> 背景）
    /// </summary>
    private async Task ToggleLrcAsync()
    {
        if (!_showLyrics && !_lyricsOnBackground)
        {
            _showLyrics = true;
            _lyricsOnBackground = false;
        }
        else if (_showLyrics && !_lyricsOnBackground)
        {
            _showLyrics = false;
            _lyricsOnBackground = true;
        }
        else
        {
            _showLyrics = false;
            _lyricsOnBackground = false;
        }

        // 保存状态
        await SaveStateAsync();
    }

    /// <summary>
    /// 切换播放列表显示
    /// </summary>
    private async Task ToggleListAsync()
    {
        _showList = !_showList;
        if (_showList && _currentIndex >= 0)
        {
            // 延迟一点以确保 DOM 渲染完成
            await Task.Delay(100);
            if (_uiController != null)
            {
                await _uiController.InvokeVoidAsync(
                    "scrollToItem",
                    $"track-item-{_currentIndex}",
                    "center"
                );
            }
        }
    }

    private bool _isPanelOpen;

    /// <summary>
    /// 切换完整播放器面板显示
    /// </summary>
    private async Task TogglePanelAsync(bool? open = null)
    {
        if (open.HasValue)
        {
            _isPanelOpen = open.Value;
        }
        else
        {
            _isPanelOpen = !_isPanelOpen;
        }

        // 通知 JS 更新 hash 和滚动条状态
        if (_uiController != null)
        {
            await _uiController.InvokeVoidAsync("setPanelOpen", _isPanelOpen);
        }
    }

    /// <summary>
    /// JS 调用：关闭面板
    /// </summary>
    [JSInvokable]
    public void ClosePanel()
    {
        _isPanelOpen = false;
        // 恢复滚动（JS 端已处理）
        StateHasChanged();
    }

    private Timer? _clickTimer;

    /// <summary>
    /// 迷你播放器点击事件（区分单击和双击）
    /// </summary>
    private void OnMiniClick()
    {
        if (_clickTimer != null)
        {
            return;
        }

        _clickTimer = new Timer(
            _ =>
            {
                _clickTimer?.Dispose();
                _clickTimer = null;
                InvokeAsync(TogglePlay);
            },
            null,
            300,
            Timeout.Infinite
        );
    }

    private void OnMiniDblClick()
    {
        if (_clickTimer != null)
        {
            _clickTimer.Change(Timeout.Infinite, Timeout.Infinite);
            _clickTimer.Dispose();
            _clickTimer = null;
        }
        InvokeAsync(async () => await TogglePanelAsync(true));
    }

    // JS 可调用方法

    /// <summary>
    /// 播放进度更新回调
    /// </summary>
    [JSInvokable]
    private void HandleTimeUpdate(double currentTime)
    {
        _currentTime = currentTime;
        if (_duration > 0)
        {
            var pct = _currentTime / _duration;
            _progressOffset = Circumference - pct * Circumference;
        }

        // 同步歌词
        if ((_showLyrics || _lyricsOnBackground) && _lyrics.Count > 0)
        {
            var activeIdx = -1;
            for (var i = 0; i < _lyrics.Count; i++)
            {
                if (_currentTime >= _lyrics[i].Time)
                {
                    activeIdx = i;
                }
                else
                {
                    break;
                }
            }

            if (activeIdx != _currentLyricIndex)
            {
                _currentLyricIndex = activeIdx;
                if (_currentLyricIndex >= 0 && _uiController != null)
                {
                    if (_showLyrics && !_lyricsOnBackground)
                    {
                        // 滚动面板内的歌词
                        _uiController.InvokeVoidAsync(
                            "scrollToItem",
                            $"lyric-line-{_currentLyricIndex}",
                            "center"
                        );
                    }
                    else if (_lyricsOnBackground)
                    {
                        // 滚动背景歌词
                        _uiController.InvokeVoidAsync("scrollToBgLyric", _currentLyricIndex);
                    }
                }
            }
        }

        StateHasChanged();
    }

    /// <summary>
    /// 播放结束回调
    /// </summary>
    [JSInvokable]
    private async void HandleEnded()
    {
        if (_playMode == PlayMode.Single)
        {
            await musicPlayerService.SetCurrentTimeAsync(0);
            await musicPlayerService.PlayAsync();
        }
        else
        {
            await Next();
        }
    }

    /// <summary>
    /// 时长改变回调
    /// </summary>
    [JSInvokable]
    private void HandleDurationChange(double duration)
    {
        _duration = duration;
        StateHasChanged();
    }

    /// <summary>
    /// 播放状态改变回调
    /// </summary>
    [JSInvokable]
    private void HandlePlayStateChange(bool isPlaying)
    {
        _isPlaying = isPlaying;
        if (_isPlaying)
        {
            StartProgressSaveTimer();
        }
        else
        {
            StopProgressSaveTimer();
            _ = SaveStateAsync();
        }
        StateHasChanged();
    }

    [JSInvokable]
    public async Task OnNext() => await Next();

    [JSInvokable]
    public async Task OnPrev() => await Prev();

    [JSInvokable]
    private void HandleError(string message)
    {
        logger.LogError("播放错误：{Message}", message);
    }

    private void HandleNextRequested() => InvokeAsync(Next);

    private void HandlePrevRequested() => InvokeAsync(Prev);

    [JSInvokable]
    public void OpenPanel()
    {
        InvokeAsync(async () => await TogglePanelAsync(true));
        StateHasChanged();
    }

    /// <summary>
    /// 格式化时间显示 (分:秒)
    /// </summary>
    private static string FormatTime(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds))
        {
            return "0:00";
        }
        var ts = TimeSpan.FromSeconds(seconds);
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
    }

    public async ValueTask DisposeAsync()
    {
        musicPlayerService.TimeUpdated -= HandleTimeUpdate;
        musicPlayerService.DurationChanged -= HandleDurationChange;
        musicPlayerService.PlayStateChanged -= HandlePlayStateChange;
        musicPlayerService.Ended -= HandleEnded;
        musicPlayerService.Error -= HandleError;
        musicPlayerService.NextRequested -= HandleNextRequested;
        musicPlayerService.PrevRequested -= HandlePrevRequested;

        StopProgressSaveTimer();

        if (_clickTimer != null)
        {
            await _clickTimer.DisposeAsync();
        }

        if (_uiController != null)
        {
            try
            {
                await _uiController.DisposeAsync();
            }
            catch (JSDisconnectedException e)
            {
                logger.LogError(e, "js 模块已断开");
            }
            catch (JSException e)
            {
                logger.LogError(e, "js 模块已异常退出");
            }
        }
        if (_uiModule != null)
        {
            try
            {
                await _uiModule.DisposeAsync();
            }
            catch (JSDisconnectedException e)
            {
                logger.LogError(e, "js 模块已断开");
            }
            catch (JSException e)
            {
                logger.LogError(e, "js 模块已异常退出");
            }
        }
        _uiRef?.Dispose();

        await musicPlayerService.DisposeAsync();
    }

    private void StartProgressSaveTimer()
    {
        if (_progressSaveTimer != null)
        {
            return;
        }

        _progressSaveTimer = new Timer(
            _ =>
            {
                if (!_isPlaying || _currentTrack == null)
                {
                    return;
                }
                _ = SaveStateAsync();
            },
            null,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5)
        );
    }

    private void StopProgressSaveTimer()
    {
        _progressSaveTimer?.Dispose();
        _progressSaveTimer = null;
    }

    [GeneratedRegex(@"\[(\d{2}):(\d{2})\.(\d{2,3})\](.*)")]
    private static partial Regex LyricRegex();
}
