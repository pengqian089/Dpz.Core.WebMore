using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;

namespace Dpz.Core.WebMore.Service.Impl;

public class GroupChatService : IGroupChatService
{
    private readonly HubConnection _hubConnection;
    private readonly ILogger<GroupChatService> _logger;

    public HubConnectionState State => _hubConnection.State;

    public UserInfo? CurrentUser { get; private set; }

    public bool IsConnected => _hubConnection.State == HubConnectionState.Connected;

    public event Action<GroupChatMessage>? OnMessageReceived;

    public event Action<UserInfo>? OnUserJoined;

    public event Action<UserInfo>? OnUserLeft;

    public event Action<List<GroupChatMessage>, bool>? OnHistoryLoaded;

    public event Action<string>? OnError;

    public event Action<object>? OnDraw;

    public event Action<UserInfo?>? OnDrawingUserChanged;

    public event Func<Exception?, Task>? OnClosed;

    public event Func<string?, Task>? OnReconnected;

    public event Func<Exception?, Task>? OnReconnecting;

    public GroupChatService(ILogger<GroupChatService> logger)
    {
        _logger = logger;

        _hubConnection = new HubConnectionBuilder()
            .WithUrl(
                $"{Program.WebHost}/groupchat",
                options =>
                {
                    options.SkipNegotiation = true;
                    options.Transports = HttpTransportType.WebSockets;
                }
            )
            .WithAutomaticReconnect()
            .Build();

        // 注册客户端方法
        _hubConnection.On<GroupChatMessage>(
            "OnMessageReceived",
            message => OnMessageReceived?.Invoke(message)
        );

        _hubConnection.On<UserInfo>(
            "OnUserJoined",
            user => OnUserJoined?.Invoke(user)
        );

        _hubConnection.On<UserInfo>(
            "OnUserLeft",
            user => OnUserLeft?.Invoke(user)
        );

        _hubConnection.On<List<GroupChatMessage>, bool>(
            "OnHistoryLoaded",
            (messages, hasMore) => OnHistoryLoaded?.Invoke(messages, hasMore)
        );

        _hubConnection.On<string>("OnError", error => OnError?.Invoke(error));

        _hubConnection.On<object>("OnDraw", data => OnDraw?.Invoke(data));

        _hubConnection.On<UserInfo?>(
            "OnDrawingUserChanged",
            user => OnDrawingUserChanged?.Invoke(user)
        );

        _hubConnection.Closed += e => OnClosed?.Invoke(e) ?? Task.CompletedTask;
        _hubConnection.Reconnected += s => OnReconnected?.Invoke(s) ?? Task.CompletedTask;
        _hubConnection.Reconnecting += e => OnReconnecting?.Invoke(e) ?? Task.CompletedTask;
    }

    public async Task StartAsync(string sessionId)
    {
        if (_hubConnection.State == HubConnectionState.Disconnected)
        {
            try
            {
                await _hubConnection.StartAsync();

                // 加入群组
                CurrentUser = await _hubConnection.InvokeAsync<UserInfo>(
                    "JoinGroup",
                    sessionId
                );

                if (CurrentUser != null)
                {
                    _logger.LogInformation("群聊连接成功，用户: {UserName}", CurrentUser.Name);
                }
                else
                {
                    _logger.LogWarning("加入群组失败，未返回用户信息");
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, "启动群聊连接失败");
                throw;
            }
        }
    }

    public async Task StopAsync()
    {
        if (_hubConnection.State != HubConnectionState.Disconnected)
        {
            try
            {
                await _hubConnection.StopAsync();
                CurrentUser = null;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "停止群聊连接失败");
            }
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        try
        {
            await _hubConnection.InvokeAsync("SendMessage", message);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "发送消息失败");
            OnError?.Invoke($"发送消息失败: {e.Message}");
        }
    }

    public async Task GetHistoryAsync(int pageIndex = 1, int pageSize = 50)
    {
        try
        {
            await _hubConnection.InvokeAsync("GetHistory", pageIndex, pageSize);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "获取历史记录失败");
            OnError?.Invoke($"获取历史记录失败: {e.Message}");
        }
    }

    public async Task RequestDrawingAccessAsync()
    {
        try
        {
            await _hubConnection.InvokeAsync("RequestDrawingAccess");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "请求画图权限失败");
            OnError?.Invoke($"请求画图权限失败: {e.Message}");
        }
    }

    public async Task ReleaseDrawingAccessAsync()
    {
        try
        {
            await _hubConnection.InvokeAsync("ReleaseDrawingAccess");
        }
        catch (Exception e)
        {
            _logger.LogError(e, "释放画图权限失败");
            OnError?.Invoke($"释放画图权限失败: {e.Message}");
        }
    }

    public async Task SendDrawingDataAsync(object data)
    {
        try
        {
            await _hubConnection.InvokeAsync("SendDrawingData", data);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "发送画图数据失败");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        await _hubConnection.DisposeAsync();
    }
}
