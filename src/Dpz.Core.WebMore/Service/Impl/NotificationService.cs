using System;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Dpz.Core.WebMore.Service.Impl;

public class NotificationService : INotificationService
{
    private readonly HubConnection _hubConnection;
    private readonly HubConnection _appHubConnection;
    private readonly ILogger<NotificationService> _logger;

    public event Action<string>? OnSystemNotification;
    public event Action<PushMessageModel>? OnPushMessage;
    public event Action<int, string>? OnPushLogMessage;
    public event Action<SubscribeModel>? OnSubscribe;
    public event Action<int, string>? OnAppSystemMessage;

    public event Func<Exception?, Task>? OnClosed;
    public event Func<string?, Task>? OnReconnected;
    public event Func<Exception?, Task>? OnReconnecting;
    public event Action<string>? OnReady;

    public HubConnectionState State => _hubConnection.State;

    public NotificationService(ILogger<NotificationService> logger)
    {
        _logger = logger;
        
        // Main Notification Connection
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

        _hubConnection.On<PushMessageModel>(
            "pushMessage",
            model =>
            {
                OnPushMessage?.Invoke(model);
            }
        );

        _hubConnection.On<int, string>(
            "pushLogMessage",
            (type, msg) =>
            {
                OnPushLogMessage?.Invoke(type, msg);
            }
        );

        _hubConnection.On<SubscribeModel>(
            "cnBetaSubscribe",
            model =>
            {
                OnSubscribe?.Invoke(model);
            }
        );

        _hubConnection.Closed += e => OnClosed?.Invoke(e) ?? Task.CompletedTask;
        _hubConnection.Reconnected += s => OnReconnected?.Invoke(s) ?? Task.CompletedTask;
        _hubConnection.Reconnecting += e => OnReconnecting?.Invoke(e) ?? Task.CompletedTask;

        // App Notification Connection
        _appHubConnection = new HubConnectionBuilder()
            .WithUrl(
                $"{Program.WebHost}/app/notification",
                options =>
                {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                }
            )
            .WithAutomaticReconnect()
            .Build();

        _appHubConnection.On<int, string>(
            "systemMessage",
            (level, msg) =>
            {
                OnAppSystemMessage?.Invoke(level, msg);
            }
        );
    }

    public async Task StartAsync()
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            try
            {
                await _hubConnection.StartAsync();
                await _hubConnection.InvokeAsync("Init");
                _logger.LogInformation("Main SignalR connection started.");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Error starting Main SignalR connection.");
            }
        }

        if (_appHubConnection.State == HubConnectionState.Disconnected)
        {
            try
            {
                await _appHubConnection.StartAsync();
                _logger.LogInformation("App SignalR connection started.");
            }
            catch (Exception e)
            {
                _logger.LogWarning(e, "Error starting App SignalR connection.");
            }
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            await _hubConnection.StopAsync();
        }

        if (_appHubConnection.State != HubConnectionState.Disconnected)
        {
            await _appHubConnection.StopAsync();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _hubConnection.DisposeAsync();
        await _appHubConnection.DisposeAsync();
    }
}
