using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Graphics;

namespace Dpz.Core.WebMore.App;

public partial class NativeMusicPlayerPage : ContentPage
{
    private readonly IMusicService _musicService;
    private readonly IMusicPlayerService _playerService;

    private readonly List<MusicModel> _musics = [];
    private readonly List<LyricLine> _lyrics = [];
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
    private readonly RingDrawable _miniRingDrawable = new();
    private const int MiniLongPressThresholdMs = 450;
    private PlayMode _playMode = PlayMode.Order;

    private enum PlayMode
    {
        Order,
        Random,
        Single,
    }

    private record LyricLine(double Time, string Text, Color Color);

    private record TrackItem(int Index, string? Title, string? Artist, string Id, bool IsActive);

    public NativeMusicPlayerPage(IMusicService musicService, IMusicPlayerService playerService)
    {
        InitializeComponent();
        _musicService = musicService;
        _playerService = playerService;
        miniRingView.Drawable = _miniRingDrawable;
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
        SetMiniMode(false);
        if (_currentIndex < 0 && _musics.Count > 0)
        {
            await LoadTrackById(_musics[0].Id, false);
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

        base.OnDisappearing();
    }

    private async Task EnsureListAsync()
    {
        if (_musics.Count > 0)
        {
            return;
        }
        var list = await _musicService.GetMusicPageAsync(1, 1000);
        _musics.AddRange(list);
    }

    private async Task LoadTrackById(string id, bool autoPlay = true)
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

        titleLabel.Text = track.Title ?? "";
        artistLabel.Text = track.Artist ?? "";
        coverImage.Source = string.IsNullOrWhiteSpace(track.CoverUrl) ? null : track.CoverUrl;
        miniCoverImage.Source = string.IsNullOrWhiteSpace(track.CoverUrl) ? null : track.CoverUrl;
        ParseLyrics(track.LyricContent);
        lyricsView.ItemsSource = _lyrics;
        bgLyricsView.ItemsSource = _lyrics;
        RefreshTrackList();

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
    }

    private void UpdatePlayButton(bool isPlaying)
    {
        _isPlaying = isPlaying;
        playButton.Text = _isPlaying ? "⏸" : "▶";
        miniPlayIcon.Text = _isPlaying ? "⏸" : "▶";
    }

    private void OnTimeUpdated(double time)
    {
        _currentTime = time;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            currentTimeLabel.Text = FormatTime(_currentTime);
            if (_duration > 0 && !_isUserSeeking)
            {
                _ignoreSeekChange = true;
                seekSlider.Value = (_currentTime / _duration) * 100;
                _ignoreSeekChange = false;
            }
            UpdateMiniProgress();
            UpdateLyricHighlight(_currentTime);
        });
    }

    private void OnDurationChanged(double duration)
    {
        _duration = duration;
        MainThread.BeginInvokeOnMainThread(() =>
        {
            durationLabel.Text = FormatTime(_duration);
            UpdateMiniProgress();
        });
    }

    private void OnPlayStateChanged(bool isPlaying)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdatePlayButton(isPlaying);
            _miniRingDrawable.IsPlaying = isPlaying;
            miniRingView.Invalidate();
        });
    }

    private async void OnEnded()
    {
        if (_playMode == PlayMode.Single && _currentIndex >= 0)
        {
            await _playerService.SetCurrentTimeAsync(0);
            await _playerService.PlayAsync();
            return;
        }
        await Next();
    }

    private void OnError(string message)
    {
    }

    private void OnNextRequested() => MainThread.BeginInvokeOnMainThread(async () => await Next());

    private void OnPrevRequested() => MainThread.BeginInvokeOnMainThread(async () => await Prev());

    private async Task Next()
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
    }

    private async Task Prev()
    {
        if (_musics.Count == 0)
        {
            return;
        }
        var prev = (_currentIndex - 1 + _musics.Count) % _musics.Count;
        await LoadTrackById(_musics[prev].Id);
    }

    private async void OnPlayPause(object sender, EventArgs e)
    {
        if (_isPlaying)
        {
            await _playerService.PauseAsync();
        }
        else
        {
            await _playerService.PlayAsync();
        }
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
        await _playerService.SetCurrentTimeAsync(time);
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
        RefreshTrackList();
    }

    private async void OnClose(object sender, EventArgs e)
    {
        await Navigation.PopModalAsync();
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

        lyricsView.IsVisible = _showLyrics;
        bgLyricsView.IsVisible = _lyricsOnBackground;
        coverFrame.IsVisible = !_showLyrics;
    }

    private void OnToggleMini(object sender, EventArgs e)
    {
        SetMiniMode(!_isMiniMode);
    }

    private void OnMiniPressed(object sender, EventArgs e)
    {
        _miniPressing = true;
        _miniLongPressTriggered = false;
        var start = DateTime.UtcNow;
        Dispatcher.StartTimer(TimeSpan.FromMilliseconds(50), () =>
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
        });
    }

    private async void OnMiniReleased(object sender, EventArgs e)
    {
        _miniPressing = false;
        if (_miniLongPressTriggered)
        {
            return;
        }

        if (_isPlaying)
        {
            await _playerService.PauseAsync();
        }
        else
        {
            await _playerService.PlayAsync();
        }
    }

    private void SetMiniMode(bool isMini)
    {
        _isMiniMode = isMini;
        fullPanel.IsVisible = !_isMiniMode;
        miniPanel.IsVisible = _isMiniMode;
        if (_isMiniMode)
        {
            listPanel.IsVisible = false;
            bgLyricsView.IsVisible = false;
            lyricsView.IsVisible = false;
            coverFrame.IsVisible = true;
        }
        else
        {
            lyricsView.IsVisible = _showLyrics;
            bgLyricsView.IsVisible = _lyricsOnBackground;
            coverFrame.IsVisible = !_showLyrics;
        }
    }

    private void UpdateMiniProgress()
    {
        var progress = _duration > 0 ? (float)(_currentTime / _duration) : 0f;
        _miniRingDrawable.Progress = Math.Clamp(progress, 0f, 1f);
        miniRingView.Invalidate();
    }

    private sealed class RingDrawable : IDrawable
    {
        public float Progress { get; set; }
        public bool IsPlaying { get; set; }

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();
            var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
            var stroke = 4f;
            var radius = size / 2 - stroke;
            var centerX = dirtyRect.Center.X;
            var centerY = dirtyRect.Center.Y;
            var rect = new RectF(centerX - radius, centerY - radius, radius * 2, radius * 2);

            canvas.StrokeSize = stroke;
            canvas.StrokeColor = Color.FromArgb("#2A2A2A");
            canvas.DrawCircle(centerX, centerY, radius);

            canvas.StrokeColor = Color.FromArgb("#FFB700");
            canvas.DrawArc(rect, -90, 360 * Progress, false, false);
            canvas.RestoreState();
        }
    }

    private void OnCycleMode(object sender, EventArgs e)
    {
        _playMode = _playMode switch
        {
            PlayMode.Order => PlayMode.Random,
            PlayMode.Random => PlayMode.Single,
            _ => PlayMode.Order
        };
        UpdateModeButton();
    }

    private void UpdateModeButton()
    {
        modeButton.Text = _playMode switch
        {
            PlayMode.Order => "顺序",
            PlayMode.Random => "随机",
            PlayMode.Single => "单曲",
            _ => "顺序"
        };
    }

    private void ParseLyrics(string? lrcContent)
    {
        _lyrics.Clear();
        _currentLyricIndex = -1;
        if (string.IsNullOrWhiteSpace(lrcContent))
        {
            _lyrics.Add(new LyricLine(0, "纯音乐 / 暂无歌词", Colors.Gray));
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
                _lyrics.Add(new LyricLine(time, text, Colors.Gray));
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

        _currentLyricIndex = index;
        for (var i = 0; i < _lyrics.Count; i++)
        {
            var line = _lyrics[i];
            var color = i == _currentLyricIndex ? Colors.White : Colors.Gray;
            _lyrics[i] = line with { Color = color };
        }

        lyricsView.ItemsSource = null;
        lyricsView.ItemsSource = _lyrics;
        bgLyricsView.ItemsSource = _lyrics;

        if (_currentLyricIndex >= 0)
        {
            var current = _lyrics[_currentLyricIndex];
            lyricsView.ScrollTo(current, position: ScrollToPosition.Center, animate: true);
            bgLyricsView.ScrollTo(current, position: ScrollToPosition.Center, animate: true);
        }
    }

    private void RefreshTrackList()
    {
        if (!_listVisible)
        {
            return;
        }

        trackList.ItemsSource = _musics.Select((m, i) => new TrackItem(
            i + 1,
            m.Title,
            m.Artist,
            m.Id,
            i == _currentIndex
        )).ToList();

        if (_currentIndex >= 0)
        {
            trackList.ScrollTo(_currentIndex, position: ScrollToPosition.Center, animate: false);
        }
    }

    [GeneratedRegex(@"\[(\d{2}):(\d{2})\.(\d{2,3})\](.*)")]
    private static partial Regex LyricRegex();

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
