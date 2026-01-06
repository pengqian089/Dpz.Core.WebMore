using System;
using System.Linq;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Shared;

public partial class MainLayout(
    INotificationService notificationService,
    IAppDialogService dialogService,
    IJSRuntime jsRuntime
) : IDisposable, IAsyncDisposable
{
    private ConnectionStatus _connectionStatus = new("", "连接状态：未连接");
    private IJSObjectReference? _jsModule;
    private IJSObjectReference? _loggerModule;
    private NotificationModel? _newsPublishBox;
    private Components.GroupChat? _groupChat;
    private int _versionClickCount;
    private DateTime _lastVersionClickTime = DateTime.MinValue;
    private string _versionClickClass = "";

    protected override async Task OnInitializedAsync()
    {
        notificationService.OnSystemNotification += HandleSystemNotification;
        notificationService.OnPushMessage += HandlePushMessage;
        notificationService.OnPushLogMessage += HandlePushLogMessage;
        notificationService.OnSubscribe += HandleSubscribe;
        notificationService.OnAppSystemMessage += HandleAppSystemMessage;

        notificationService.OnReconnecting += HandleReconnecting;
        notificationService.OnReconnected += HandleReconnected;
        notificationService.OnClosed += HandleClosed;
        notificationService.OnReady += HandleReady;

        await notificationService.StartAsync();

        try
        {
            _jsModule = await jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./js/modules/notification-interop.js"
            );
            _loggerModule = await jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./js/modules/console-logger.js"
            );
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error loading JS module: {e.Message}");
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            // 通知 JS：Blazor 已启动并完成首次渲染
            try
            {
                await jsRuntime.InvokeVoidAsync("notifyBlazorStarted");
            }
            catch (Exception)
            {
                // JS 可能还没准备好，忽略错误
            }
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private void HandlePushMessage(PushMessageModel model)
    {
        _ = InvokeAsync(async () =>
        {
            if (_jsModule != null)
            {
                await _jsModule.InvokeVoidAsync(
                    "requestBrowserNotification",
                    "小喇叭开始广播辣",
                    model.Markdown
                );
            }

            if (_loggerModule != null)
            {
                await _loggerModule.InvokeVoidAsync("outPutInfo", model.Markdown);
            }
        });
    }

    private void HandlePushLogMessage(int type, string message)
    {
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var toastType = type switch
        {
            0 => ToastType.Success,
            1 => ToastType.Warning,
            2 => ToastType.Error,
            _ => ToastType.Info,
        };

        _ = InvokeAsync(async () =>
        {
            if (_loggerModule != null)
            {
                var funcName = type switch
                {
                    0 => "outPutSuccess",
                    1 => "outPutWarning",
                    2 => "outPutError",
                    _ => "outPutInfo",
                };
                await _loggerModule.InvokeVoidAsync(funcName, message);
            }
            else
            {
                Console.WriteLine($"[{toastType}] {message}");
            }
        });

        dialogService.Toast($"{timestamp}\n{message}", toastType);
        StateHasChanged();
    }

    private void HandleAppSystemMessage(int level, string message)
    {
        var toastType = level switch
        {
            0 => ToastType.Success,
            1 => ToastType.Warning,
            2 => ToastType.Error,
            _ => ToastType.Info,
        };

        _ = InvokeAsync(async () =>
        {
            if (_loggerModule != null)
            {
                var funcName = level switch
                {
                    0 => "outPutSuccess",
                    1 => "outPutWarning",
                    2 => "outPutError",
                    _ => "outPutInfo",
                };
                await _loggerModule.InvokeVoidAsync(funcName, message);
            }
            else
            {
                Console.WriteLine($"[AppSystem] {message}");
            }
        });

        dialogService.Toast(message, toastType);
        StateHasChanged();
    }

    private void HandleSubscribe(SubscribeModel model)
    {
        var values = model.ProgressValues.Select(x => x * 100).ToArray();

        if (_newsPublishBox == null)
        {
            _newsPublishBox = dialogService.ShowNotification(
                new NotificationOptions
                {
                    Title = "新闻订阅发布",
                    Content = model.Message,
                    Bars = values,
                    Type = NotificationType.Info,
                }
            );
        }
        else
        {
            _newsPublishBox.UpdateContent?.Invoke(model.Message);
            _newsPublishBox.UpdateProgress?.Invoke(values);
        }

        switch (model.Type)
        {
            case 3:
                // Success and Close
                _ = Task.Delay(1000)
                    .ContinueWith(_ =>
                    {
                        InvokeAsync(() =>
                        {
                            _newsPublishBox?.Close?.Invoke();
                            _newsPublishBox = null;
                            StateHasChanged();
                        });
                    });
                break;
        }
        StateHasChanged();
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
        _ = InvokeAsync(async () =>
        {
            if (_loggerModule != null)
            {
                await _loggerModule.InvokeVoidAsync("outPutInfo", message);
            }
            else
            {
                Console.WriteLine(message);
            }
        });
        StateHasChanged();
    }

    public void Dispose()
    {
        notificationService.OnSystemNotification -= HandleSystemNotification;
        notificationService.OnPushMessage -= HandlePushMessage;
        notificationService.OnPushLogMessage -= HandlePushLogMessage;
        notificationService.OnSubscribe -= HandleSubscribe;
        notificationService.OnAppSystemMessage -= HandleAppSystemMessage;

        notificationService.OnReconnecting -= HandleReconnecting;
        notificationService.OnReconnected -= HandleReconnected;
        notificationService.OnClosed -= HandleClosed;
        notificationService.OnReady -= HandleReady;
    }

    public async ValueTask DisposeAsync()
    {
        if (_jsModule != null)
        {
            await _jsModule.DisposeAsync();
        }
        if (_loggerModule != null)
        {
            await _loggerModule.DisposeAsync();
        }
    }

    private async Task HandleVersionClick()
    {
        var now = DateTime.Now;

        // 如果距离上次点击超过2秒，重置计数
        if ((now - _lastVersionClickTime).TotalSeconds > 2)
        {
            _versionClickCount = 0;
        }

        _lastVersionClickTime = now;
        _versionClickCount++;

        // 添加点击反馈
        _versionClickClass = "version-text--clicked";
        StateHasChanged();

        // 移除点击反馈样式
        _ = Task.Delay(200)
            .ContinueWith(_ =>
            {
                InvokeAsync(() =>
                {
                    _versionClickClass = "";
                    StateHasChanged();
                });
            });

        // 如果点击了10次，打开群聊
        if (_versionClickCount >= 10)
        {
            _versionClickCount = 0;
            if (_groupChat != null)
            {
                await _groupChat.OpenAsync();
            }
        }
    }

    private readonly record struct ConnectionStatus(string Class, string Title);
}
