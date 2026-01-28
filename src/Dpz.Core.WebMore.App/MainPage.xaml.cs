using Dpz.Core.WebMore.App.Pages.Tools;
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
    private bool _miniPressing;
    private bool _miniLongPressTriggered;
    private const int MiniLongPressThresholdMs = 450;

    public MainPage()
    {
        InitializeComponent();
        BlazorWebViewInstance = blazorWebView;
        CurrentInstance = this;

        // Setup mini player drawables
        mainMiniOuterRingView.Drawable = _miniDrawables.OuterRing;
        mainMiniProgressRingView.Drawable = _miniDrawables.ProgressRing;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        // Get player service and subscribe to events
        _playerService = IPlatformApplication.Current?.Services.GetService<IMusicPlayerService>();
        if (_playerService != null)
        {
            _playerService.PlayStateChanged += OnPlayerStateChanged;
        }
    }

    protected override void OnDisappearing()
    {
        if (_playerService != null)
        {
            _playerService.PlayStateChanged -= OnPlayerStateChanged;
        }
        base.OnDisappearing();
    }

    private void OnPlayerStateChanged(bool isPlaying)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            mainMiniPlayIcon.Text = isPlaying ? FontAwesomeIcons.Pause : FontAwesomeIcons.Play;
        });
    }

    public void UpdateMiniPlayer(string? coverUrl, double progress, bool isPlaying)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            mainMiniCoverImage.Source = string.IsNullOrWhiteSpace(coverUrl) ? null : coverUrl;
            _miniDrawables.ProgressRing.Progress = (float)Math.Clamp(progress, 0, 1);
            mainMiniProgressRingView.Invalidate();
            mainMiniPlayIcon.Text = isPlaying ? FontAwesomeIcons.Pause : FontAwesomeIcons.Play;
        });
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
        var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2f + 3f;

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
        var radius = Math.Min(dirtyRect.Width, dirtyRect.Height) / 2f - 2f;
        var rect = new RectF(centerX - radius, centerY - radius, radius * 2, radius * 2);

        canvas.StrokeSize = 3f;
        canvas.StrokeColor = Color.FromArgb("#FFB700");
        canvas.StrokeLineCap = LineCap.Butt;
        canvas.DrawArc(rect, -90, 360 * Progress, false, false);

        canvas.RestoreState();
    }
}
