using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service;

public interface INotificationService : IAsyncDisposable
{
    HubConnectionState State { get; }

    event Action<string>? OnSystemNotification;
    
    event Action<PushMessageModel>? OnPushMessage;
    
    event Action<int, string>? OnPushLogMessage;
    
    event Action<SubscribeModel>? OnSubscribe;

    event Action<int, string>? OnAppSystemMessage;

    event Func<Exception?, Task>? OnClosed;

    event Func<string?, Task>? OnReconnected;

    event Func<Exception?, Task>? OnReconnecting;

    event Action<string>? OnReady;

    Task StartAsync();

    Task StopAsync();
}
