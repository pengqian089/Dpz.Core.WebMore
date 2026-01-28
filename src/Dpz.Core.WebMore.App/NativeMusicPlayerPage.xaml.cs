using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Serilog;

// ReSharper disable UnusedAutoPropertyAccessor.Local

namespace Dpz.Core.WebMore.App;

public partial class NativeMusicPlayerPage : ContentPage
{
    private readonly IMusicService _musicService;
    private readonly IMusicPlayerService _playerService;

    private readonly List<MusicModel> _musics = [];
    private readonly ObservableCollection<LyricLineViewModel> _lyrics = [];
    private int _currentIndex = -1;
    private int _currentLyricIndex = -1;
    private bool _isPlaying;
    private double _currentTime;
    private double _duration;
    private bool _listVisible;
    private bool _showLyrics;
    private bool _lyricsOnBackground;
    private bool _ignoreSeekChange;
    private bool _isUserSeeking;
    private bool _isMiniMode;
    private bool _miniPressing;
    private bool _miniLongPressTriggered;

    private readonly ReactorRing1Drawable _ring1Drawable = new();
    private readonly ReactorRing2Drawable _ring2Drawable = new();
    private readonly ReactorRing3Drawable _ring3Drawable = new();
    private readonly MiniOuterRingDrawable _miniOuterRingDrawable = new();
    private readonly MiniProgressRingDrawable _miniProgressRingDrawable = new();

    private CancellationTokenSource? _animationCts;
    private const int MiniLongPressThresholdMs = 450;
    private PlayMode _playMode = PlayMode.Order;

    private enum PlayMode
    {
        Order,
        Random,
        Single,
    }

    private class LyricLineViewModel : BindableObject
    {
        public double Time { get; set; }
        public string Text { get; set; } = string.Empty;

        private Color _color = Colors.Gray;
        public Color Color
        {
            get => _color;
            set
            {
                if (_color != value)
                {
                    _color = value;
                    OnPropertyChanged();
                }
            }
        }

        private int _fontSize = 14;
        public int FontSize
        {
            get => _fontSize;
            set
            {
                if (_fontSize != value)
                {
                    _fontSize = value;
                    OnPropertyChanged();
                }
            }
        }

        private double _opacity = 0.5;
        public double Opacity
        {
            get => _opacity;
            set
            {
                if (Math.Abs(_opacity - value) > 0.001)
                {
                    _opacity = value;
                    OnPropertyChanged();
                }
            }
        }

        private FontAttributes _fontWeight = FontAttributes.None;
        public FontAttributes FontWeight
        {
            get => _fontWeight;
            set
            {
                if (_fontWeight != value)
                {
                    _fontWeight = value;
                    OnPropertyChanged();
                }
            }
        }
    }

    private record TrackItem(int Index, string? Title, string? Artist, string Id, bool IsActive);

    public NativeMusicPlayerPage(IMusicService musicService, IMusicPlayerService playerService)
    {
        InitializeComponent();
        _musicService = musicService;
        _playerService = playerService;

        // Set drawables
        ring1View.Drawable = _ring1Drawable;
        ring2View.Drawable = _ring2Drawable;
        ring3View.Drawable = _ring3Drawable;
        miniOuterRingView.Drawable = _miniOuterRingDrawable;
        miniProgressRingView.Drawable = _miniProgressRingDrawable;

        // Initialize progress to 0
        _miniProgressRingDrawable.Progress = 0f;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _playerService.TimeUpdated += OnTimeUpdated;
        _playerService.DurationChanged += OnDurationChanged;
        _playerService.PlayStateChanged += OnPlayStateChanged;
        _playerService.Ended += OnEnded;
        _playerService.Error += OnError;
        _playerService.NextRequested += OnNextRequested;
        _playerService.PrevRequested += OnPrevRequested;

        await _playerService.InitializeAsync();
        await EnsureListAsync();

        // Try to restore last state
        if (_currentIndex < 0 && _musics.Count > 0)
        {
            var savedState = await _playerService.LoadStateAsync();
            var trackId = savedState?.TrackId;
            var mainState = MainPage.CurrentInstance;
            if (
                mainState != null
                && mainState.IsPlayerInitialized
                && !string.IsNullOrEmpty(mainState.CurrentTrackId)
            )
            {
                trackId = mainState.CurrentTrackId;
            }
            if (string.IsNullOrEmpty(trackId) && _musics.Count > 0)
            {
                trackId = _musics[0].Id;
            }

            // Restore play mode
            if (
                !string.IsNullOrEmpty(savedState?.PlayModeStr)
                && Enum.TryParse<PlayMode>(savedState.PlayModeStr, out var mode)
            )
            {
                _playMode = mode;
            }

            // Restore lyrics settings
            if (savedState != null)
            {
                _showLyrics = savedState.ShowLyrics;
                _lyricsOnBackground = savedState.LyricsOnBackground;
                UpdateLyricsVisibility();
            }

            var index = !string.IsNullOrEmpty(trackId)
                ? _musics.FindIndex(m => m.Id == trackId)
                : -1;
            if (index < 0)
            {
                index = 0;
            }

            if (
                mainState != null
                && mainState.IsPlayerInitialized
                && mainState.CurrentTrackId == trackId
            )
            {
                // Player already has the correct track, just sync UI state
                var track = _musics[index];
                _currentIndex = index;
                ApplyTrackUi(track, updateMainPage: false);
                _currentTime = mainState.CurrentTime;
                _duration = mainState.Duration;
                UpdateTimeUi();
                UpdatePlayButton(mainState.IsPlaying);
                UpdateLyricHighlight(_currentTime);
            }
            else if (!string.IsNullOrEmpty(trackId))
            {
                // Load saved track
                await LoadTrackById(trackId, false);

                // Restore playback position if valid
                if (
                    savedState != null
                    && savedState.CurrentTime > 0
                    && savedState.CurrentTime < _duration
                )
                {
                    await _playerService.SetCurrentTimeAsync(savedState.CurrentTime);
                }
            }
        }

        UpdateModeButton();
    }

    protected override void OnDisappearing()
    {
        _playerService.TimeUpdated -= OnTimeUpdated;
        _playerService.DurationChanged -= OnDurationChanged;
        _playerService.PlayStateChanged -= OnPlayStateChanged;
        _playerService.Ended -= OnEnded;
        _playerService.Error -= OnError;
        _playerService.NextRequested -= OnNextRequested;
        _playerService.PrevRequested -= OnPrevRequested;

        StopAnimations();

        // Save current state
        var trackId =
            _currentIndex >= 0 && _currentIndex < _musics.Count ? _musics[_currentIndex].Id : null;
        if (!string.IsNullOrEmpty(trackId))
        {
            _ = _playerService.SaveStateAsync(
                new MusicPlayerState(
                    _playMode.ToString(),
                    _showLyrics,
                    _lyricsOnBackground,
                    trackId,
                    _currentTime
                )
            );
        }

        base.OnDisappearing();
    }

    private async Task EnsureListAsync()
    {
        if (_musics.Count > 0)
        {
            return;
        }

        await RunSafeAsync(
            async () =>
            {
                var list = await _musicService.GetMusicPageAsync(1, 1000);
                _musics.AddRange(list);
            },
            "加载播放列表"
        );
    }

    private async Task LoadTrackById(string id, bool autoPlay = true)
    {
        await RunSafeAsync(
            async () =>
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    return;
                }

                var index = _musics.FindIndex(m => m.Id == id);
                if (index < 0 || index >= _musics.Count)
                {
                    return;
                }

                _currentIndex = index;
                var track = _musics[_currentIndex];
                ApplyTrackUi(track);

                // Reset progress
                _currentTime = 0;
                _duration = 0;
                _miniProgressRingDrawable.Progress = 0f;
                miniProgressRingView.Invalidate();
                UpdateMiniProgress();

                await _playerService.SetSourceAsync(track.MusicUrl, track.Id);
                await _playerService.UpdateMediaSessionAsync(
                    track.Title ?? string.Empty,
                    track.Artist ?? string.Empty,
                    track.CoverUrl ?? string.Empty
                );

                if (autoPlay)
                {
                    await _playerService.PlayAsync();
                }
                else
                {
                    UpdatePlayButton(false);
                }
            },
            "加载音乐"
        );
    }

    private void ApplyTrackUi(MusicModel track, bool updateMainPage = true)
    {
        titleLabel.Text = track.Title ?? "";
        artistLabel.Text = track.Artist ?? "";
        coverImage.Source = string.IsNullOrWhiteSpace(track.CoverUrl) ? null : track.CoverUrl;
        miniCoverImage.Source = string.IsNullOrWhiteSpace(track.CoverUrl) ? null : track.CoverUrl;
        ParseLyrics(track.LyricContent);
        lyricsView.ItemsSource = _lyrics;
        RefreshTrackList();
        if (updateMainPage)
        {
            MainPage.CurrentInstance?.SetCurrentTrack(track.Id, track.CoverUrl);
        }
    }

    private void UpdateTimeUi()
    {
        currentTimeLabel.Text = FormatTime(_currentTime);
        durationLabel.Text = FormatTime(_duration);
        if (_duration > 0 && !_isUserSeeking)
        {
            _ignoreSeekChange = true;
            seekSlider.Value = (_currentTime / _duration) * 100;
            _ignoreSeekChange = false;
        }
        UpdateMiniProgress();
    }

    private void UpdatePlayButton(bool isPlaying)
    {
        _isPlaying = isPlaying;
        playButton.Text = _isPlaying ? FontAwesomeIcons.Pause : FontAwesomeIcons.Play;
        miniPlayIcon.Text = _isPlaying ? FontAwesomeIcons.Pause : FontAwesomeIcons.Play;

        if (_isPlaying)
        {
            StartAnimations();
        }
        else
        {
            StopAnimations();
        }
    }

    private void OnTimeUpdated(double time)
    {
        _currentTime = time;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateTimeUi();
            UpdateLyricHighlight(_currentTime);

            // Update MainPage mini player
            var progress = _duration > 0 ? _currentTime / _duration : 0;
            var coverUrl =
                _currentIndex >= 0 && _currentIndex < _musics.Count
                    ? _musics[_currentIndex].CoverUrl
                    : null;
            MainPage.CurrentInstance?.UpdateMiniPlayer(coverUrl, progress, _isPlaying);
        });
    }

    private void OnDurationChanged(double duration)
    {
        _duration = duration;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateTimeUi();
        });
    }

    private void OnPlayStateChanged(bool isPlaying)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdatePlayButton(isPlaying);
        });
    }

    private async void OnEnded()
    {
        await RunSafeAsync(
            async () =>
            {
                if (_playMode == PlayMode.Single && _currentIndex >= 0)
                {
                    await _playerService.SetCurrentTimeAsync(0);
                    await _playerService.PlayAsync();
                    return;
                }
                await Next();
            },
            "播放结束处理"
        );
    }

    private void OnError(string message) { }

    private void OnNextRequested() => MainThread.BeginInvokeOnMainThread(async () => await Next());

    private void OnPrevRequested() => MainThread.BeginInvokeOnMainThread(async () => await Prev());

    private async Task Next()
    {
        await RunSafeAsync(
            async () =>
            {
                if (_musics.Count == 0)
                {
                    return;
                }
                int next;
                if (_playMode == PlayMode.Random)
                {
                    next = _musics.Count == 1 ? 0 : new Random().Next(_musics.Count);
                }
                else
                {
                    next = (_currentIndex + 1) % _musics.Count;
                }
                await LoadTrackById(_musics[next].Id);
            },
            "切换下一首"
        );
    }

    private async Task Prev()
    {
        await RunSafeAsync(
            async () =>
            {
                if (_musics.Count == 0)
                {
                    return;
                }
                var prev = (_currentIndex - 1 + _musics.Count) % _musics.Count;
                await LoadTrackById(_musics[prev].Id);
            },
            "切换上一首"
        );
    }

    private async void OnPlayPause(object sender, EventArgs e)
    {
        await RunSafeAsync(
            async () =>
            {
                if (_isPlaying)
                {
                    await _playerService.PauseAsync();
                }
                else
                {
                    await _playerService.PlayAsync();
                }
            },
            "播放/暂停"
        );
    }

    private async void OnNext(object sender, EventArgs e) => await Next();

    private async void OnPrev(object sender, EventArgs e) => await Prev();

    private async void OnSeekChanged(object sender, ValueChangedEventArgs e)
    {
        if (_ignoreSeekChange)
        {
            return;
        }
        if (_duration <= 0)
        {
            return;
        }
        var time = _duration * (e.NewValue / 100.0);
        await RunSafeAsync(() => _playerService.SetCurrentTimeAsync(time), "进度调整");
    }

    private void OnSeekDragStarted(object sender, EventArgs e)
    {
        _isUserSeeking = true;
    }

    private void OnSeekDragCompleted(object sender, EventArgs e)
    {
        _isUserSeeking = false;
    }

    private void OnToggleList(object sender, EventArgs e)
    {
        SetMiniMode(false);
        _listVisible = !_listVisible;
        listPanel.IsVisible = _listVisible;

        // Update button style
        listButton.BorderColor = _listVisible ? Color.FromArgb("#FFB700") : Colors.Transparent;
        listButton.TextColor = _listVisible ? Color.FromArgb("#FFB700") : Color.FromArgb("#A69E96");

        if (_listVisible)
        {
            RefreshTrackList();
        }
    }

    private async void OnClose(object sender, EventArgs e)
    {
        await RunSafeAsync(() => Navigation.PopModalAsync(), "关闭播放器");
    }

    private void OnTrackSelected(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not TrackItem item)
        {
            return;
        }

        var index = _musics.FindIndex(m => m.Id == item.Id);
        if (index >= 0)
        {
            _ = LoadTrackById(_musics[index].Id);
        }
        trackList.SelectedItem = null;
    }

    private void OnToggleLyrics(object sender, EventArgs e)
    {
        SetMiniMode(false);

        // Cycle through three modes: Off -> Panel -> Background -> Off
        if (!_showLyrics && !_lyricsOnBackground)
        {
            // Mode 1: Show in panel
            _showLyrics = true;
            _lyricsOnBackground = false;
        }
        else if (_showLyrics && !_lyricsOnBackground)
        {
            // Mode 2: Show on background
            _showLyrics = false;
            _lyricsOnBackground = true;
        }
        else
        {
            // Mode 3: Hide all
            _showLyrics = false;
            _lyricsOnBackground = false;
        }

        UpdateLyricsVisibility();
        UpdateLyricHighlight(_currentTime);
    }

    private void UpdateLyricsVisibility()
    {
        lyricsView.IsVisible = _showLyrics;
        reactorContainer.IsVisible = !_showLyrics;
        bgLyricsContainer.IsVisible = _lyricsOnBackground;

        // Update button style
        var isActive = _showLyrics || _lyricsOnBackground;
        lyricButton.BorderColor = isActive ? Color.FromArgb("#FFB700") : Colors.Transparent;
        lyricButton.TextColor = isActive ? Color.FromArgb("#FFB700") : Color.FromArgb("#A69E96");
    }

    private async void OnToggleMini(object sender, EventArgs e)
    {
        // 最小化时关闭Modal页面返回MainPage
        await Navigation.PopModalAsync();
    }

    private void OnMiniPressed(object sender, EventArgs e)
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
                    SetMiniMode(false);
                    return false;
                }

                return true;
            }
        );
    }

    private async void OnMiniReleased(object sender, EventArgs e)
    {
        _miniPressing = false;
        if (_miniLongPressTriggered)
        {
            return;
        }

        await RunSafeAsync(
            async () =>
            {
                if (_isPlaying)
                {
                    await _playerService.PauseAsync();
                }
                else
                {
                    await _playerService.PlayAsync();
                }
            },
            "迷你播放/暂停"
        );
    }

    private void SetMiniMode(bool isMini)
    {
        _isMiniMode = isMini;
        fullPanel.IsVisible = !_isMiniMode;
        miniPanel.IsVisible = _isMiniMode;

        if (_isMiniMode)
        {
            listPanel.IsVisible = false;
            // 在Mini模式下，如果启用了背景歌词，仍然显示
            bgLyricsContainer.IsVisible = _lyricsOnBackground;
        }
        else
        {
            UpdateLyricsVisibility();
        }
    }

    private void UpdateMiniProgress()
    {
        var progress = _duration > 0 ? (float)(_currentTime / _duration) : 0f;
        _miniProgressRingDrawable.Progress = Math.Clamp(progress, 0f, 1f);
        miniProgressRingView.Invalidate();
    }

    // Animation Methods
    private void StartAnimations()
    {
        StopAnimations();
        _animationCts = new CancellationTokenSource();
        var token = _animationCts.Token;

        // Start rotation animations
        Task.Run(
            async () =>
            {
                var ring1Angle = 0f;
                var ring2Angle = 0f;
                var ring3Angle = 0f;
                var miniOuterAngle = 0f;

                while (!token.IsCancellationRequested)
                {
                    // Update rotation angles
                    ring1Angle = (ring1Angle + 0.5f) % 360f; // Slow rotation reverse
                    ring2Angle = (ring2Angle + 1.5f) % 360f; // Medium rotation
                    ring3Angle = (ring3Angle + 2f) % 360f; // Fast rotation with pulse
                    miniOuterAngle = (miniOuterAngle + 1f) % 360f;

                    _ring1Drawable.RotationAngle = -ring1Angle; // Reverse
                    _ring2Drawable.RotationAngle = ring2Angle;
                    _ring3Drawable.RotationAngle = ring3Angle;
                    _miniOuterRingDrawable.RotationAngle = miniOuterAngle;

                    // Pulse effect for ring 3
                    var pulsePhase = (ring3Angle / 180f) % 2f;
                    _ring3Drawable.Scale =
                        0.95f + (pulsePhase > 1f ? (2f - pulsePhase) : pulsePhase) * 0.1f;

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ring1View.Invalidate();
                        ring2View.Invalidate();
                        ring3View.Invalidate();
                        miniOuterRingView.Invalidate();
                    });

                    await Task.Delay(30, token); // ~33 FPS
                }
            },
            token
        );
    }

    private void StopAnimations()
    {
        _animationCts?.Cancel();
        _animationCts?.Dispose();
        _animationCts = null;

        // Reset angles
        _ring1Drawable.RotationAngle = 0;
        _ring2Drawable.RotationAngle = 0;
        _ring3Drawable.RotationAngle = 0;
        _ring3Drawable.Scale = 1f;
        _miniOuterRingDrawable.RotationAngle = 0;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            ring1View.Invalidate();
            ring2View.Invalidate();
            ring3View.Invalidate();
            miniOuterRingView.Invalidate();
        });
    }

    // Drawable Classes
    private sealed class ReactorRing1Drawable : IDrawable
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
            canvas.StrokeDashPattern = new float[] { 10, 5 };
            canvas.Alpha = 0.3f;
            canvas.DrawCircle(centerX, centerY, radius);

            canvas.RestoreState();
        }
    }

    private sealed class ReactorRing2Drawable : IDrawable
    {
        public float RotationAngle { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            var centerX = dirtyRect.Center.X;
            var centerY = dirtyRect.Center.Y;
            var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2f * 0.85f;

            canvas.Translate(centerX, centerY);
            canvas.Rotate(RotationAngle);
            canvas.Translate(-centerX, -centerY);

            canvas.StrokeSize = 1f;
            canvas.StrokeColor = Color.FromArgb("#00F3FF");
            canvas.Alpha = 0.5f;

            // Draw partial circle (left and right arcs only)
            var rect = new RectF(centerX - radius, centerY - radius, radius * 2, radius * 2);
            canvas.DrawArc(rect, 45, 90, false, false);
            canvas.DrawArc(rect, 225, 90, false, false);

            canvas.RestoreState();
        }
    }

    private sealed class ReactorRing3Drawable : IDrawable
    {
        public float RotationAngle { get; set; }
        public float Scale { get; set; } = 1f;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            var centerX = dirtyRect.Center.X;
            var centerY = dirtyRect.Center.Y;
            var baseRadius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2f * 0.72f;
            var radius = baseRadius * Scale;

            canvas.Translate(centerX, centerY);
            canvas.Rotate(RotationAngle);
            canvas.Scale(Scale, Scale);
            canvas.Translate(-centerX, -centerY);

            canvas.StrokeSize = 4f;
            canvas.StrokeColor = Color.FromArgb("#FFB700");
            canvas.StrokeDashPattern = new float[] { 2, 3 };
            canvas.Alpha = 0.4f;
            canvas.DrawCircle(centerX, centerY, radius / Scale); // Compensate for scale

            canvas.RestoreState();
        }
    }

    private sealed class MiniOuterRingDrawable : IDrawable
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

    private sealed class MiniProgressRingDrawable : IDrawable
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

    private void OnCycleMode(object sender, EventArgs e)
    {
        _playMode = _playMode switch
        {
            PlayMode.Order => PlayMode.Random,
            PlayMode.Random => PlayMode.Single,
            _ => PlayMode.Order,
        };
        UpdateModeButton();
    }

    private void UpdateModeButton()
    {
        modeButton.Text = _playMode switch
        {
            PlayMode.Order => FontAwesomeIcons.SortAmountAsc, // 顺序播放
            PlayMode.Random => FontAwesomeIcons.Random, // 随机播放
            PlayMode.Single => FontAwesomeIcons.Retweet, // 单曲循环
            _ => FontAwesomeIcons.SortAmountAsc,
        };

        var tooltip = _playMode switch
        {
            PlayMode.Order => "顺序播放",
            PlayMode.Random => "随机播放",
            PlayMode.Single => "单曲循环",
            _ => "顺序播放",
        };
        ToolTipProperties.SetText(modeButton, tooltip);
    }

    private void ParseLyrics(string? lrcContent)
    {
        _lyrics.Clear();
        _currentLyricIndex = -1;
        if (string.IsNullOrWhiteSpace(lrcContent))
        {
            _lyrics.Add(
                new LyricLineViewModel
                {
                    Time = 0,
                    Text = "纯音乐 / 暂无歌词",
                    Color = Colors.Gray,
                    FontSize = 14,
                    Opacity = 0.5,
                }
            );
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
                _lyrics.Add(
                    new LyricLineViewModel
                    {
                        Time = time,
                        Text = text,
                        Color = Colors.Gray,
                        FontSize = 14,
                        Opacity = 0.5,
                    }
                );
            }
        }
    }

    private void UpdateLyricHighlight(double time)
    {
        if (_lyrics.Count == 0)
        {
            return;
        }

        var index = -1;
        for (var i = 0; i < _lyrics.Count; i++)
        {
            if (_lyrics[i].Time <= time)
            {
                index = i;
            }
            else
            {
                break;
            }
        }

        if (index == _currentLyricIndex)
        {
            return;
        }

        var oldIndex = _currentLyricIndex;
        _currentLyricIndex = index;

        // Update only changed items to avoid flickering
        if (oldIndex >= 0 && oldIndex < _lyrics.Count)
        {
            var oldLine = _lyrics[oldIndex];
            oldLine.Color = Colors.Gray;
            oldLine.FontSize = _lyricsOnBackground ? 20 : 14;
            oldLine.Opacity = 0.5;
            oldLine.FontWeight = FontAttributes.None;
        }

        if (_currentLyricIndex >= 0 && _currentLyricIndex < _lyrics.Count)
        {
            var currentLine = _lyrics[_currentLyricIndex];
            currentLine.Color = _lyricsOnBackground ? Color.FromArgb("#FFB700") : Colors.White;
            currentLine.FontSize = _lyricsOnBackground ? 24 : 16;
            currentLine.Opacity = 1.0;
            currentLine.FontWeight = FontAttributes.Bold;
        }

        // Scroll to current lyric with smooth animation
        if (_currentLyricIndex >= 0 && _currentLyricIndex < _lyrics.Count)
        {
            var current = _lyrics[_currentLyricIndex];
            if (_showLyrics)
            {
                lyricsView.ScrollTo(current, position: ScrollToPosition.Center, animate: true);
            }
        }

        // Update background lyrics (two lines)
        if (_lyricsOnBackground)
        {
            string currentText = "纯音乐 / 暂无歌词";
            string? nextText = null;

            if (_currentLyricIndex >= 0 && _currentLyricIndex < _lyrics.Count)
            {
                currentText = _lyrics[_currentLyricIndex].Text;
                bgCurrentLyric.Text = currentText;

                // Show next line
                if (_currentLyricIndex + 1 < _lyrics.Count)
                {
                    nextText = _lyrics[_currentLyricIndex + 1].Text;
                    bgNextLyric.Text = nextText;
                    bgNextLyric.IsVisible = true;
                }
                else
                {
                    bgNextLyric.IsVisible = false;
                }
            }
            else
            {
                bgCurrentLyric.Text = currentText;
                bgNextLyric.IsVisible = false;
            }

            // Update MainPage background lyrics
            MainPage.CurrentInstance?.UpdateBackgroundLyrics(true, currentText, nextText);
        }
        else
        {
            // Hide MainPage background lyrics
            MainPage.CurrentInstance?.UpdateBackgroundLyrics(false, "", null);
        }
    }

    private void RefreshTrackList()
    {
        if (!_listVisible)
        {
            return;
        }

        trackList.ItemsSource = _musics
            .Select((m, i) => new TrackItem(i + 1, m.Title, m.Artist, m.Id, i == _currentIndex))
            .ToList();

        if (_currentIndex >= 0)
        {
            trackList.ScrollTo(_currentIndex, position: ScrollToPosition.Center, animate: false);
        }
    }

    [GeneratedRegex(@"\[(\d{2}):(\d{2})\.(\d{2,3})\](.*)")]
    private static partial Regex LyricRegex();

    private async Task RunSafeAsync(Func<Task> action, string actionName)
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "播放器操作失败：{ActionName}", actionName);
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await DisplayAlertAsync("播放器", $"操作失败：{actionName}", "确定");
            });
        }
    }

    // private Task<bool> DisplayAlertAsync(string title, string message, string cancel)
    // {
    //     return DisplayAlertAsync(title, message, cancel);
    // }

    private static string FormatTime(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds))
        {
            return "0:00";
        }
        var ts = TimeSpan.FromSeconds(seconds);
        return $"{(int)ts.TotalMinutes}:{ts.Seconds:D2}";
    }
}
