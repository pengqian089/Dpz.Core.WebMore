#if ANDROID
using System.Text.Json;
using Android.Content;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Dpz.Core.WebMore.Service;

namespace Dpz.Core.WebMore.App.Platforms.Android;

public sealed class AndroidMusicPlayerService : Java.Lang.Object, IMusicPlayerService
{
    private const string StateKey = "dpz_music_player_state";
    private readonly Context _context = global::Android.App.Application.Context;
    private MediaPlayer? _player;
    private MediaSession? _mediaSession;
    private Timer? _tickTimer;
    private bool _isPrepared;
    private string? _currentTrackId;

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

        _player = new MediaPlayer();
        _player.SetAudioAttributes(
            new AudioAttributes.Builder()
                .SetContentType(AudioContentType.Music)
                ?.SetUsage(AudioUsageKind.Media)
                ?.Build()
        );
        _player.SetWakeMode(_context, WakeLockFlags.Partial);
        _player.Completion += OnCompletion;
        _player.Error += OnError;
        _player.Prepared += OnPrepared;

        _mediaSession = new MediaSession(_context, "Dpz.Core.Music");
        _mediaSession.SetCallback(new MediaSessionCallback(this));
        _mediaSession.Active = true;

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
        _player.Reset();

        var uri =
            global::Android.Net.Uri.Parse(url) ?? throw new ArgumentException(
                "Invalid audio url",
                nameof(url)
            );
        await _player.SetDataSourceAsync(_context, uri);
        await PrepareAsync();
    }

    public Task PlayAsync()
    {
        if (_player == null || !_isPrepared)
        {
            return Task.CompletedTask;
        }

        _player.Start();
        StartTickTimer();
        PlayStateChanged?.Invoke(true);
        UpdatePlaybackState(PlaybackStateCode.Playing);
        return Task.CompletedTask;
    }

    public Task PauseAsync()
    {
        if (_player == null)
        {
            return Task.CompletedTask;
        }

        if (_player.IsPlaying)
        {
            _player.Pause();
        }
        StopTickTimer();
        PlayStateChanged?.Invoke(false);
        UpdatePlaybackState(PlaybackStateCode.Paused);
        return Task.CompletedTask;
    }

    public Task SetCurrentTimeAsync(double time)
    {
        if (_player == null || !_isPrepared)
        {
            return Task.CompletedTask;
        }

        _player.SeekTo((int)(time * 1000));
        return Task.CompletedTask;
    }

    public Task UpdateMediaSessionAsync(string title, string artist, string coverUrl)
    {
        if (_mediaSession == null)
        {
            return Task.CompletedTask;
        }

        var metadata = new MediaMetadata.Builder()
            .PutString(MediaMetadata.MetadataKeyTitle, title)
            ?.PutString(MediaMetadata.MetadataKeyArtist, artist)
            ?.Build();
        _mediaSession.SetMetadata(metadata);
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

    private Task PrepareAsync()
    {
        var tcs = new TaskCompletionSource();
        void Handler(object? sender, EventArgs args)
        {
            _player!.Prepared -= Handler;
            tcs.TrySetResult();
        }

        _player!.Prepared += Handler;
        _player.PrepareAsync();
        return tcs.Task;
    }

    private void OnPrepared(object? sender, EventArgs e)
    {
        _isPrepared = true;
        if (_player != null)
        {
            DurationChanged?.Invoke(_player.Duration / 1000.0);
        }
        UpdatePlaybackState(PlaybackStateCode.Paused);
    }

    private void OnCompletion(object? sender, EventArgs e)
    {
        StopTickTimer();
        Ended?.Invoke();
        UpdatePlaybackState(PlaybackStateCode.Paused);
    }

    private void OnError(object? sender, MediaPlayer.ErrorEventArgs e)
    {
        Error?.Invoke($"MediaPlayer error: {e.What}/{e.Extra}");
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
                if (_player == null || !_player.IsPlaying)
                {
                    return;
                }
                TimeUpdated?.Invoke(_player.CurrentPosition / 1000.0);
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

    private void UpdatePlaybackState(PlaybackStateCode state)
    {
        if (_mediaSession == null || _player == null)
        {
            return;
        }

        var playbackState = new PlaybackState.Builder()
            .SetState(state, _player.CurrentPosition, 1.0f)
            ?.SetActions(
                PlaybackState.ActionPlay
                    | PlaybackState.ActionPause
                    | PlaybackState.ActionSkipToNext
                    | PlaybackState.ActionSkipToPrevious
            )
            ?.Build();

        _mediaSession.SetPlaybackState(playbackState);
    }

    public async ValueTask DisposeAsync()
    {
        StopTickTimer();

        if (_player != null)
        {
            try
            {
                if (_player.IsPlaying)
                {
                    _player.Stop();
                }

                _player.Release();
            }
            catch
            {
                // ignore
            }
        }

        if (_mediaSession != null)
        {
            _mediaSession.Active = false;
            _mediaSession.Release();
        }

        _player?.Dispose();
        _mediaSession?.Dispose();
        await Task.CompletedTask;
    }

    private sealed class MediaSessionCallback(AndroidMusicPlayerService service)
        : MediaSession.Callback
    {
        public override void OnSkipToNext() => service.NextRequested?.Invoke();

        public override void OnSkipToPrevious() => service.PrevRequested?.Invoke();

        public override void OnPlay() => _ = service.PlayAsync();

        public override void OnPause() => _ = service.PauseAsync();
    }
}
#endif
