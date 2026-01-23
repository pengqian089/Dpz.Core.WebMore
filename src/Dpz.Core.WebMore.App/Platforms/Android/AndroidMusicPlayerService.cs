#if ANDROID
#pragma warning disable CA1416
#pragma warning disable CS0618
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Android.Content;
using Android.Media;
using Android.Media.Session;
using Android.OS;
using Android.Runtime;
using Dpz.Core.WebMore.Service;
using Microsoft.Maui.Storage;

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
    private bool _playWhenReady;
    private bool _resumeAfterFocusGain;
    private bool _isDucked;
    private AudioManager? _audioManager;
#if ANDROID
    private AudioFocusRequestClass? _audioFocusRequest;
    private AudioFocusChangeListener? _audioFocusChangeListener;
#endif

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

        _audioManager = _context.GetSystemService(Context.AudioService) as AudioManager;
        _audioFocusChangeListener ??= new AudioFocusChangeListener(this);

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
        if (_player == null)
        {
            return Task.CompletedTask;
        }

        if (!_isPrepared)
        {
            _playWhenReady = true;
            return Task.CompletedTask;
        }

        RequestAudioFocus();

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
        _playWhenReady = false;
        StopTickTimer();
        PlayStateChanged?.Invoke(false);
        UpdatePlaybackState(PlaybackStateCode.Paused);
        AbandonAudioFocus();
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

        if (_playWhenReady)
        {
            _playWhenReady = false;
            _ = PlayAsync();
        }
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
        AbandonAudioFocus();

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

    private void RequestAudioFocus()
    {
        if (_audioManager == null)
        {
            return;
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            _audioFocusRequest ??= new AudioFocusRequestClass.Builder(AudioFocus.Gain)
                .SetAudioAttributes(
                    new AudioAttributes.Builder()
                        .SetContentType(AudioContentType.Music)!
                        .SetUsage(AudioUsageKind.Media)!
                        .Build()!
                )
                .SetOnAudioFocusChangeListener(_audioFocusChangeListener)
                .Build();

#pragma warning disable CA1416
            if (_audioFocusRequest != null)
            {
                _audioManager.RequestAudioFocus(_audioFocusRequest);
            }
#pragma warning restore CA1416
        }
        else
        {
            // API < 26: 不请求音频焦点以避免过时 API 警告
        }
    }

    private void AbandonAudioFocus()
    {
        if (_audioManager == null)
        {
            return;
        }

        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            if (_audioFocusRequest != null)
            {
#pragma warning disable CA1416
                _audioManager.AbandonAudioFocusRequest(_audioFocusRequest);
#pragma warning restore CA1416
            }
        }
        else
        {
            // API < 26: 不释放音频焦点
        }
    }

    private sealed class MediaSessionCallback(AndroidMusicPlayerService service)
        : MediaSession.Callback
    {
        public override void OnSkipToNext() => service.NextRequested?.Invoke();

        public override void OnSkipToPrevious() => service.PrevRequested?.Invoke();

        public override void OnPlay() => _ = service.PlayAsync();

        public override void OnPause() => _ = service.PauseAsync();
    }

#if ANDROID
    private sealed class AudioFocusChangeListener(AndroidMusicPlayerService service)
        : Java.Lang.Object, AudioManager.IOnAudioFocusChangeListener
    {
        public void OnAudioFocusChange([GeneratedEnum] AudioFocus focusChange)
        {
            switch (focusChange)
            {
                case AudioFocus.Gain:
                    if (service._isDucked && service._player != null)
                    {
                        service._player.SetVolume(1f, 1f);
                        service._isDucked = false;
                    }
                    if (service._resumeAfterFocusGain)
                    {
                        service._resumeAfterFocusGain = false;
                        _ = service.PlayAsync();
                    }
                    break;
                case AudioFocus.Loss:
                    service._resumeAfterFocusGain = false;
                    _ = service.PauseAsync();
                    break;
                case AudioFocus.LossTransient:
                    if (service._player?.IsPlaying == true)
                    {
                        service._resumeAfterFocusGain = true;
                    }
                    _ = service.PauseAsync();
                    break;
                case AudioFocus.LossTransientCanDuck:
                    if (service._player != null)
                    {
                        service._player.SetVolume(0.2f, 0.2f);
                        service._isDucked = true;
                    }
                    break;
            }
        }
    }
#endif
}
#endif
