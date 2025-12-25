using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace Dpz.Core.WebMore.Service;

public interface INotificationService : IAsyncDisposable
{
    HubConnectionState State { get; }

    event Action<string>? OnSystemNotification;

    event Func<Exception?, Task>? OnClosed;

    event Func<string?, Task>? OnReconnected;

    event Func<Exception?, Task>? OnReconnecting;

    event Action<string>? OnReady;

    Task StartAsync();

    Task StopAsync();
}
