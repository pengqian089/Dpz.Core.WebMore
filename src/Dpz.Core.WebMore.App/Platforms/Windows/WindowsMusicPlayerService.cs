using System.Text.Json;
using Dpz.Core.WebMore.Service;
using Windows.Media.Core;
using Windows.Media.Playback;

namespace Dpz.Core.WebMore.App.Platforms.Windows;

public sealed class WindowsMusicPlayerService : IMusicPlayerService
{
    private const string StateKey = "dpz_music_player_state";
    private MediaPlayer? _player;
    private Timer? _tickTimer;
    private bool _isPrepared;
    private string? _currentTrackId;
    private bool _playWhenReady;

    public event Action<double>? TimeUpdated;
    public event Action<double>? DurationChanged;
    public event Action<bool>? PlayStateChanged;
    public event Action? Ended;
    public event Action<string>? Error;
    public event Action? NextRequested;
    public event Action? PrevRequested;

    public Task InitializeAsync()
    {
        if (_player != null)
        {
            return Task.CompletedTask;
        }

        _player = new MediaPlayer
        {
            AudioCategory = MediaPlayerAudioCategory.Media,
            AutoPlay = false,
        };

        _player.MediaOpened += OnMediaOpened;
        _player.MediaEnded += OnMediaEnded;
        _player.MediaFailed += OnMediaFailed;

        return Task.CompletedTask;
    }

    public async Task SetSourceAsync(string url, string? trackId = null)
    {
        if (_player == null)
        {
            return;
        }

        _currentTrackId = trackId;
        _isPrepared = false;

        try
        {
            var mediaSource = MediaSource.CreateFromUri(new Uri(url));
            _player.Source = mediaSource;
            await Task.Delay(100); // Give time for media to load
        }
        catch (Exception ex)
        {
            Error?.Invoke($"Failed to set source: {ex.Message}");
        }
    }

    public Task PlayAsync()
    {
        if (_player == null)
        {
            return Task.CompletedTask;
        }

        if (!_isPrepared)
        {
            _playWhenReady = true;
            return Task.CompletedTask;
        }

        _player.Play();
        StartTickTimer();
        PlayStateChanged?.Invoke(true);
        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        if (_player == null)
        {
            return Task.CompletedTask;
        }

        _player.Pause();
        _playWhenReady = false;
        StopTickTimer();
        PlayStateChanged?.Invoke(false);
        return Task.CompletedTask;
    }

    public Task SetCurrentTimeAsync(double time)
    {
        if (_player == null || !_isPrepared)
        {
            return Task.CompletedTask;
        }

        _player.Position = TimeSpan.FromSeconds(time);
        return Task.CompletedTask;
    }

    public Task UpdateMediaSessionAsync(string title, string artist, string coverUrl)
    {
        // Windows doesn't have a direct equivalent to Android's MediaSession in .NET MAUI
        // System Media Transport Controls would be used in UWP/WinUI3, but it's more complex in MAUI
        // For now, we'll keep it simple
        return Task.CompletedTask;
    }

    public Task SaveStateAsync(MusicPlayerState state)
    {
        var json = JsonSerializer.Serialize(state);
        Preferences.Set(StateKey, json);
        return Task.CompletedTask;
    }

    public Task<MusicPlayerState?> LoadStateAsync()
    {
        var json = Preferences.Get(StateKey, string.Empty);
        if (string.IsNullOrWhiteSpace(json))
        {
            return Task.FromResult<MusicPlayerState?>(null);
        }

        try
        {
            var state = JsonSerializer.Deserialize<MusicPlayerState>(json);
            return Task.FromResult(state);
        }
        catch
        {
            return Task.FromResult<MusicPlayerState?>(null);
        }
    }

    private void OnMediaOpened(MediaPlayer sender, object args)
    {
        _isPrepared = true;

        var duration = sender.PlaybackSession.NaturalDuration;
        DurationChanged?.Invoke(duration.TotalSeconds);

        if (_playWhenReady)
        {
            _playWhenReady = false;
            _ = PlayAsync();
        }
    }

    private void OnMediaEnded(MediaPlayer sender, object args)
    {
        StopTickTimer();
        Ended?.Invoke();
    }

    private void OnMediaFailed(MediaPlayer sender, MediaPlayerFailedEventArgs args)
    {
        Error?.Invoke($"Media playback failed: {args.ErrorMessage}");
    }

    private void StartTickTimer()
    {
        if (_tickTimer != null || _player == null)
        {
            return;
        }

        _tickTimer = new Timer(
            _ =>
            {
                if (
                    _player == null
                    || _player.PlaybackSession.PlaybackState != MediaPlaybackState.Playing
                )
                {
                    return;
                }
                TimeUpdated?.Invoke(_player.Position.TotalSeconds);
            },
            null,
            TimeSpan.Zero,
            TimeSpan.FromMilliseconds(500)
        );
    }

    private void StopTickTimer()
    {
        _tickTimer?.Dispose();
        _tickTimer = null;
    }

    public async ValueTask DisposeAsync()
    {
        StopTickTimer();

        if (_player != null)
        {
            try
            {
                _player.Pause();
                _player.MediaOpened -= OnMediaOpened;
                _player.MediaEnded -= OnMediaEnded;
                _player.MediaFailed -= OnMediaFailed;
                _player.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        await Task.CompletedTask;
    }
}
