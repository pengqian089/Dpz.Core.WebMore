using System;
using System.Threading.Tasks;

namespace Dpz.Core.WebMore.Service;

public record MusicPlayerState(
    string PlayModeStr,
    bool ShowLyrics,
    bool LyricsOnBackground,
    string? TrackId,
    double CurrentTime
);

public interface IMusicPlayerService : IAsyncDisposable
{
    event Action<double>? TimeUpdated;
    event Action<double>? DurationChanged;
    event Action<bool>? PlayStateChanged;
    event Action? Ended;
    event Action<string>? Error;
    event Action? NextRequested;
    event Action? PrevRequested;

    Task InitializeAsync();
    Task SetSourceAsync(string url, string? trackId = null);
    Task PlayAsync();
    Task PauseAsync();
    Task SetCurrentTimeAsync(double time);
    Task UpdateMediaSessionAsync(string title, string artist, string coverUrl);
    Task SaveStateAsync(MusicPlayerState state);
    Task<MusicPlayerState?> LoadStateAsync();
}