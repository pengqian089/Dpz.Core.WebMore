using System.Text.RegularExpressions;
using Dpz.Core.WebMore.App.Pages.Tools;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Serilog;

namespace Dpz.Core.WebMore.App;

public partial class MainPage : ContentPage
{
    public static BlazorWebView? BlazorWebViewInstance { get; private set; }
    public static MainPage? CurrentInstance { get; private set; }

    private readonly MiniPlayerDrawables _miniDrawables = new();
    private IMusicPlayerService? _playerService;
    private IMusicService? _musicService;
    private readonly List<MusicModel> _musics = [];
    private bool _miniPressing;
    private bool _miniLongPressTriggered;
    private bool _isAnimating;
    private const int MiniLongPressThresholdMs = 450;
    private bool _miniInitialized;
    private bool _isPlaying;
    private double _miniCurrentTime;
    private double _miniDuration;
    private double? _pendingSeekTime;
    private string? _currentTrackId;
    private string? _currentCoverUrl;
    private string _lastBgLyric = string.Empty;
    private bool _bgAnimating;
    private bool _lyricsOnBackground;
    private readonly List<LyricLine> _bgLyrics = [];
    private int _bgLyricIndex = -1;

    public bool IsPlayerInitialized => _miniInitialized;
    public string? CurrentTrackId => _currentTrackId;
    public double CurrentTime => _miniCurrentTime;
    public double Duration => _miniDuration;
    public bool IsPlaying => _isPlaying;

    private record LyricLine(double Time, string Text);

    public MainPage()
    {
        InitializeComponent();
        BlazorWebViewInstance = blazorWebView;
        CurrentInstance = this;

        // Setup mini player drawables
        mainMiniOuterRingView.Drawable = _miniDrawables.OuterRing;
        mainMiniProgressRingView.Drawable = _miniDrawables.ProgressRing;

        // Initialize progress to 0
        _miniDrawables.ProgressRing.Progress = 0f;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Get player service and subscribe to events
        _playerService = IPlatformApplication.Current?.Services.GetService<IMusicPlayerService>();
        _musicService = IPlatformApplication.Current?.Services.GetService<IMusicService>();
        if (_playerService != null)
        {
            _playerService.PlayStateChanged += OnPlayerStateChanged;
            _playerService.TimeUpdated += OnPlayerTimeUpdated;
            _playerService.DurationChanged += OnPlayerDurationChanged;

            // Initialize player service
            await _playerService.InitializeAsync();

            await EnsureMiniPlayerInitializedAsync();
        }

        if (_isPlaying)
        {
            StartMiniAnimation();
        }
        else
        {
            StopMiniAnimation();
        }
    }

    private void OnPlayerTimeUpdated(double time)
    {
        _miniCurrentTime = time;
        if (!_isPlaying)
        {
            _isPlaying = true;
            mainMiniPlayIcon.Text = FontAwesomeIcons.Pause;
            StartMiniAnimation();
        }
        var progress = _miniDuration > 0 ? _miniCurrentTime / _miniDuration : 0;
        UpdateMiniPlayer(_currentCoverUrl, progress, _isPlaying);
        UpdateBackgroundLyricsByTime();
    }

    private async void OnPlayerDurationChanged(double duration)
    {
        _miniDuration = duration;
        if (_pendingSeekTime.HasValue && _playerService != null && _miniDuration > 0)
        {
            var seekTime = Math.Min(_pendingSeekTime.Value, _miniDuration);
            _pendingSeekTime = null;
            try
            {
                _miniCurrentTime = seekTime;
                await _playerService.SetCurrentTimeAsync(seekTime);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "设置初始播放进度失败");
            }
        }
        UpdateMiniPlayer(
            _currentCoverUrl,
            _miniDuration > 0 ? _miniCurrentTime / _miniDuration : 0,
            _isPlaying
        );
        UpdateBackgroundLyricsByTime();
    }

    protected override void OnDisappearing()
    {
        if (_playerService != null)
        {
            _playerService.PlayStateChanged -= OnPlayerStateChanged;
            _playerService.TimeUpdated -= OnPlayerTimeUpdated;
            _playerService.DurationChanged -= OnPlayerDurationChanged;
        }

        StopMiniAnimation();
        base.OnDisappearing();
    }

    private void OnPlayerStateChanged(bool isPlaying)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _isPlaying = isPlaying;
            mainMiniPlayIcon.Text = isPlaying ? FontAwesomeIcons.Pause : FontAwesomeIcons.Play;

            if (isPlaying)
            {
                StartMiniAnimation();
            }
            else
            {
                StopMiniAnimation();
            }
        });
    }

    private void StartMiniAnimation()
    {
        if (_isAnimating)
        {
            return;
        }

        _isAnimating = true;

        mainMiniCoverImage.AbortAnimation("MiniCoverRotate");
        mainMiniPlayIcon.AbortAnimation("MiniIconRotate");

        var coverAnimation = new Animation(v => mainMiniCoverImage.Rotation = v, 0, 360);
        mainMiniCoverImage.Animate(
            "MiniCoverRotate",
            coverAnimation,
            16,
            5000,
            Easing.Linear,
            repeat: () => _isAnimating
        );

        var iconAnimation = new Animation(v => mainMiniPlayIcon.Rotation = v, 0, 360);
        mainMiniPlayIcon.Animate(
            "MiniIconRotate",
            iconAnimation,
            16,
            5000,
            Easing.Linear,
            repeat: () => _isAnimating
        );

        var angle = 0f;
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(16), () =>
        {
            if (!_isAnimating)
            {
                return false;
            }

            angle = (angle + 1f) % 360f;
            _miniDrawables.OuterRing.RotationAngle = angle;
            mainMiniOuterRingView.Invalidate();
            return true;
        });
    }

    private void StopMiniAnimation()
    {
        _isAnimating = false;
        mainMiniCoverImage.AbortAnimation("MiniCoverRotate");
        mainMiniPlayIcon.AbortAnimation("MiniIconRotate");

        _miniDrawables.OuterRing.RotationAngle = 0;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            mainMiniOuterRingView.Invalidate();
            mainMiniCoverImage.Rotation = 0;
            mainMiniPlayIcon.Rotation = 0;
        });
    }

    private async Task EnsureMiniPlayerInitializedAsync()
    {
        if (_miniInitialized || _playerService == null || _musicService == null)
        {
            return;
        }

        await EnsureMusicListAsync();

        if (_musics.Count == 0)
        {
            return;
        }

        var savedState = await _playerService.LoadStateAsync();
        MusicModel? track = null;
        if (savedState != null && !string.IsNullOrEmpty(savedState.TrackId))
        {
            track = _musics.FirstOrDefault(m => m.Id == savedState.TrackId);
        }
        track ??= _musics.FirstOrDefault();

        if (track == null)
        {
            return;
        }

        _currentTrackId = track.Id;
        _currentCoverUrl = track.CoverUrl;
        _miniCurrentTime = 0;
        _miniDuration = 0;
        _miniDrawables.ProgressRing.Progress = 0f;
        mainMiniProgressRingView.Invalidate();
        UpdateMiniPlayer(_currentCoverUrl, 0, false);

        await _playerService.SetSourceAsync(track.MusicUrl, track.Id);
        await _playerService.UpdateMediaSessionAsync(
            track.Title ?? string.Empty,
            track.Artist ?? string.Empty,
            track.CoverUrl ?? string.Empty
        );

        if (savedState != null && savedState.CurrentTime > 0)
        {
            _pendingSeekTime = savedState.CurrentTime;
        }
        _lyricsOnBackground = savedState?.LyricsOnBackground ?? false;
        ParseBackgroundLyrics(track.LyricContent);
        UpdateBackgroundLyricsByTime();

        _miniInitialized = true;
    }

    private async Task EnsureMusicListAsync()
    {
        if (_musics.Count > 0 || _musicService == null)
        {
            return;
        }

        try
        {
            var list = await _musicService.GetMusicPageAsync(1, 1000);
            _musics.AddRange(list);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载播放列表失败");
        }
    }

    public void SetCurrentTrack(string? trackId, string? coverUrl)
    {
        _currentTrackId = trackId;
        _currentCoverUrl = coverUrl;
        _miniCurrentTime = 0;
        _miniDuration = 0;
        _miniDrawables.ProgressRing.Progress = 0f;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            mainMiniProgressRingView.Invalidate();
        });

        var track = _musics.FirstOrDefault(m => m.Id == trackId);
        if (track != null)
        {
            ParseBackgroundLyrics(track.LyricContent);
        }

        MainThread.BeginInvokeOnMainThread(() =>
        {
            mainMiniCoverImage.Source = string.IsNullOrWhiteSpace(coverUrl) ? null : coverUrl;
        });
    }

    public void UpdateMiniPlayer(string? coverUrl, double progress, bool isPlaying)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _currentCoverUrl = coverUrl;
            _isPlaying = isPlaying;
            mainMiniCoverImage.Source = string.IsNullOrWhiteSpace(coverUrl) ? null : coverUrl;
            _miniDrawables.ProgressRing.Progress = (float)Math.Clamp(progress, 0, 1);
            mainMiniProgressRingView.Invalidate();
            mainMiniPlayIcon.Text = isPlaying ? FontAwesomeIcons.Pause : FontAwesomeIcons.Play;
        });
    }

    public void UpdateBackgroundLyrics(bool show, string currentLyric, string? nextLyric)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _lyricsOnBackground = show;
            mainBgLyricsContainer.IsVisible = show;
            if (show)
            {
                if (_lastBgLyric != currentLyric)
                {
                    _lastBgLyric = currentLyric;
                    _ = AnimateBackgroundLyricsAsync(currentLyric, nextLyric);
                }
                else
                {
                    mainBgCurrentLyric.Text = currentLyric;
                    if (!string.IsNullOrEmpty(nextLyric))
                    {
                        mainBgNextLyric.Text = nextLyric;
                        mainBgNextLyric.IsVisible = true;
                    }
                    else
                    {
                        mainBgNextLyric.IsVisible = false;
                    }
                }
            }
            else
            {
                _lastBgLyric = string.Empty;
            }
        });
    }

    private void UpdateBackgroundLyricsByTime()
    {
        if (!_lyricsOnBackground)
        {
            UpdateBackgroundLyrics(false, string.Empty, null);
            return;
        }

        if (_bgLyrics.Count == 0)
        {
            UpdateBackgroundLyrics(true, "纯音乐 / 暂无歌词", null);
            return;
        }

        var index = -1;
        for (var i = 0; i < _bgLyrics.Count; i++)
        {
            if (_bgLyrics[i].Time <= _miniCurrentTime)
            {
                index = i;
            }
            else
            {
                break;
            }
        }

        if (index == _bgLyricIndex)
        {
            return;
        }

        _bgLyricIndex = index;
        var current = index >= 0 ? _bgLyrics[index].Text : "纯音乐 / 暂无歌词";
        var next = index + 1 < _bgLyrics.Count ? _bgLyrics[index + 1].Text : null;
        UpdateBackgroundLyrics(true, current, next);
    }

    private void ParseBackgroundLyrics(string? lrcContent)
    {
        _bgLyrics.Clear();
        _bgLyricIndex = -1;
        if (string.IsNullOrWhiteSpace(lrcContent))
        {
            return;
        }

        var lines = lrcContent.Split('\n');
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
            var ms = msStr.Length == 3 ? int.Parse(msStr) : int.Parse(msStr) * 10;
            var time = min * 60 + sec + ms / 1000.0;
            var text = match.Groups[4].Value.Trim();

            if (!string.IsNullOrEmpty(text))
            {
                _bgLyrics.Add(new LyricLine(time, text));
            }
        }
    }

    [GeneratedRegex(@"\[(\d{2}):(\d{2})\.(\d{2,3})\](.*)")]
    private static partial Regex LyricRegex();

    private async Task AnimateBackgroundLyricsAsync(string currentLyric, string? nextLyric)
    {
        if (_bgAnimating)
        {
            return;
        }

        _bgAnimating = true;

        mainBgCurrentLyric.Text = currentLyric;
        if (!string.IsNullOrEmpty(nextLyric))
        {
            mainBgNextLyric.Text = nextLyric;
            mainBgNextLyric.IsVisible = true;
        }
        else
        {
            mainBgNextLyric.IsVisible = false;
        }

        mainBgCurrentLyric.TranslationY = 12;
        mainBgCurrentLyric.Opacity = 0;
        mainBgNextLyric.TranslationY = 12;

        await Task.WhenAll(
            mainBgCurrentLyric.TranslateTo(0, 0, 220, Easing.CubicOut),
            mainBgCurrentLyric.FadeTo(1, 220, Easing.CubicOut),
            mainBgNextLyric.TranslateTo(0, 0, 220, Easing.CubicOut)
        );

        _bgAnimating = false;
    }

    private async void OnMiniPlayerClick(object sender, EventArgs e)
    {
        if (_miniLongPressTriggered)
            return;

        // Toggle play/pause
        if (_playerService != null)
        {
            try
            {
                if (mainMiniPlayIcon.Text == FontAwesomeIcons.Pause)
                {
                    await _playerService.PauseAsync();
                }
                else
                {
                    await _playerService.PlayAsync();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Mini播放器播放/暂停失败");
            }
        }
    }

    private void OnMiniPlayerPressed(object sender, EventArgs e)
    {
        _miniPressing = true;
        _miniLongPressTriggered = false;
        var start = DateTime.UtcNow;

        Dispatcher.StartTimer(
            TimeSpan.FromMilliseconds(50),
            () =>
            {
                if (!_miniPressing)
                {
                    return false;
                }

                if ((DateTime.UtcNow - start).TotalMilliseconds >= MiniLongPressThresholdMs)
                {
                    _miniLongPressTriggered = true;
                    MainThread.BeginInvokeOnMainThread(async () =>
                    {
                        var page =
                            IPlatformApplication.Current?.Services.GetService<NativeMusicPlayerPage>();
                        if (page != null)
                        {
                            await Navigation.PushModalAsync(page);
                        }
                    });
                    return false;
                }

                return true;
            }
        );
    }

    private void OnMiniPlayerReleased(object sender, EventArgs e)
    {
        _miniPressing = false;
    }

    private async void OnOpenLogViewer(object sender, EventArgs e)
    {
        try
        {
            var page = IPlatformApplication.Current?.Services.GetService<LogViewerPage>();
            if (page != null)
            {
                await Navigation.PushAsync(page);
            }
            else
            {
                await DisplayAlertAsync("日志", "日志页面未注册。", "确定");
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "打开日志页面失败");
            await DisplayAlertAsync("日志", $"打开日志页面失败：{ex.Message}", "确定");
        }
    }
}

// Mini player drawable classes
public class MiniPlayerDrawables
{
    public MiniOuterRingDrawable OuterRing { get; } = new();
    public MiniProgressRingDrawable ProgressRing { get; } = new();
}

public class MiniOuterRingDrawable : IDrawable
{
    public float RotationAngle { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        var centerX = dirtyRect.Center.X;
        var centerY = dirtyRect.Center.Y;
        var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2f - 2f;

        canvas.Translate(centerX, centerY);
        canvas.Rotate(RotationAngle);
        canvas.Translate(-centerX, -centerY);

        canvas.StrokeSize = 2f;
        canvas.StrokeColor = Color.FromArgb("#FFB700");
        canvas.StrokeDashPattern = new float[] { 8, 4 };
        canvas.Alpha = 0.6f;
        canvas.DrawCircle(centerX, centerY, radius);

        canvas.RestoreState();
    }
}

public class MiniProgressRingDrawable : IDrawable
{
    public float Progress { get; set; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.SaveState();
        var centerX = dirtyRect.Center.X;
        var centerY = dirtyRect.Center.Y;
        var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2f - 4f;
        var rect = new RectF(centerX - radius, centerY - radius, radius * 2, radius * 2);
        var startAngle = -90f;
        var sweepAngle = 360f * Math.Clamp(Progress, 0f, 1f);
        if (float.IsNaN(sweepAngle) || sweepAngle <= 0f)
        {
            canvas.RestoreState();
            return;
        }

        canvas.StrokeSize = 3f;
        canvas.StrokeColor = Color.FromArgb("#FFB700");
        canvas.StrokeLineCap = LineCap.Butt;
        if (sweepAngle >= 359.9f)
        {
            canvas.DrawCircle(centerX, centerY, radius);
        }
        else
        {
            canvas.DrawArc(rect, startAngle, startAngle + sweepAngle, false, false);
        }

        canvas.RestoreState();
    }
}
