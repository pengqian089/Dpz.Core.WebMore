using System;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Service;

namespace Dpz.Core.WebMore.Shared;

public partial class MainLayout(
    INotificationService notificationService,
    IAppDialogService dialogService
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
        dialogService.Toast(msg);
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

    private readonly record struct ConnectionStatus(string Class, string Title);
}
