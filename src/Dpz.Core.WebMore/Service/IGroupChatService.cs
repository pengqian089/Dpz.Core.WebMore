using System;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Microsoft.AspNetCore.SignalR.Client;

namespace Dpz.Core.WebMore.Service;

public interface IGroupChatService : IAsyncDisposable
{
    HubConnectionState State { get; }

    Dpz.Core.WebMore.Models.UserInfo? CurrentUser { get; }

    bool IsConnected { get; }

    event Action<GroupChatMessage>? OnMessageReceived;

    event Action<Dpz.Core.WebMore.Models.UserInfo>? OnUserJoined;

    event Action<Dpz.Core.WebMore.Models.UserInfo>? OnUserLeft;

    event Action<System.Collections.Generic.List<GroupChatMessage>, bool>? OnHistoryLoaded;

    event Action<string>? OnError;

    event Action<object>? OnDraw;

    event Action<Dpz.Core.WebMore.Models.UserInfo?>? OnDrawingUserChanged;

    event Func<Exception?, Task>? OnClosed;

    event Func<string?, Task>? OnReconnected;

    event Func<Exception?, Task>? OnReconnecting;

    Task StartAsync(string sessionId);

    Task StopAsync();

    Task SendMessageAsync(string message);

    Task GetHistoryAsync(int pageIndex = 1, int pageSize = 50);

    Task RequestDrawingAccessAsync();

    Task ReleaseDrawingAccessAsync();

    Task SendDrawingDataAsync(object data);
}
