using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Dpz.Core.WebMore.Service.Impl;

public class NotificationService : INotificationService
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<NotificationService> _logger;

    public event Action<string>? OnSystemNotification;
    public event Func<Exception?, Task>? OnClosed;
    public event Func<string?, Task>? OnReconnected;
    public event Func<Exception?, Task>? OnReconnecting;
    public event Action<string>? OnReady;

    public HubConnectionState State => _hubConnection.State;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
        _hubConnection = new HubConnectionBuilder()
            .WithUrl(
                $"{Program.WebHost}/notification",
                options =>
                {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                }
            )
            .WithAutomaticReconnect()
            .Build();

        _hubConnection.On<string>(
            "systemNotification",
            msg =>
            {
                OnSystemNotification?.Invoke(msg);
            }
        );

        _hubConnection.On<string>(
            "ready",
            msg =>
            {
                OnReady?.Invoke(msg);
            }
        );

        _hubConnection.Closed += e => OnClosed?.Invoke(e) ?? Task.CompletedTask;
        _hubConnection.Reconnected += s => OnReconnected?.Invoke(s) ?? Task.CompletedTask;
        _hubConnection.Reconnecting += e => OnReconnecting?.Invoke(e) ?? Task.CompletedTask;
    }

    public async Task StartAsync()
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            try
            {
                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("Init");
                _logger.LogInformation("SignalR connection started.");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Error starting SignalR connection.");
            }
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            await _hubConnection.StopAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
    }
}
