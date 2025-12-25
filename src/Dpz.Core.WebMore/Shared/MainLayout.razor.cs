using System;
using System.Linq;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Service;
using MudBlazor;

namespace Dpz.Core.WebMore.Shared;

public partial class MainLayout(
    IMusicService musicService,
    INotificationService notificationService,
    ISnackbar snackbar
) : IDisposable
{
    private ConnectionStatus _connectionStatus = new("", "连接状态：未连接");

    protected override async Task OnInitializedAsync()
    {
        notificationService.OnSystemNotification += HandleSystemNotification;
        notificationService.OnReconnecting += HandleReconnecting;
        notificationService.OnReconnected += HandleReconnected;
        notificationService.OnClosed += HandleClosed;
        notificationService.OnReady += HandleReady;

        await notificationService.StartAsync();
        await base.OnInitializedAsync();
    }

    private void HandleSystemNotification(string msg)
    {
        snackbar.Add(msg);
        StateHasChanged();
    }

    private Task HandleReconnecting(Exception? arg)
    {
        _connectionStatus = new ConnectionStatus(
            "connection-status--disconnected",
            "消息服务：正在重新连接..."
        );
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task HandleReconnected(string? arg)
    {
        _connectionStatus = new ConnectionStatus(
            "connection-status--connected",
            "消息服务：已连接"
        );
        StateHasChanged();
        return Task.CompletedTask;
    }

    private Task HandleClosed(Exception? arg)
    {
        _connectionStatus = new ConnectionStatus(
            "connection-status--disconnected",
            "消息服务：已断开"
        );
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void HandleReady(string message)
    {
        _connectionStatus = new ConnectionStatus(
            "connection-status--connected",
            "消息服务：已连接"
        );
        Console.WriteLine(message);
        StateHasChanged();
    }

    public void Dispose()
    {
        notificationService.OnSystemNotification -= HandleSystemNotification;
        notificationService.OnReconnecting -= HandleReconnecting;
        notificationService.OnReconnected -= HandleReconnected;
        notificationService.OnClosed -= HandleClosed;
        notificationService.OnReady -= HandleReady;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        //await JsRuntime.InvokeVoidAsync("appInit");
        if (firstRender)
        {
            //await JsRuntime.InvokeVoidAsync("playerInit");
            var musics = await musicService.GetMusicPageAsync(1, 50);
            var list = musics.Select(x => new
            {
                artist = x.Artist,
                cover = x.CoverUrl,
                lrc = x.LyricUrl,
                name = x.Title,
                url = x.MusicUrl,
            });
            //await JsRuntime.InvokeVoidAsync("playerAddList", list);
            StateHasChanged();
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private readonly record struct ConnectionStatus(string Class, string Title);
}
