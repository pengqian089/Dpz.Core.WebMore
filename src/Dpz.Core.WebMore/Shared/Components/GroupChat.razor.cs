using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Shared.Components;

public partial class GroupChat(
    IGroupChatService groupChatService,
    IAppDialogService dialogService,
    IJSRuntime jsRuntime
) : ComponentBase, IDisposable, IAsyncDisposable
{
    private bool _isVisible;
    private string _currentUserId = "";
    private string _inputMessage = "";
    private bool _isSending;
    private bool _showCommands;
    private readonly List<GroupChatMessage> _messages = [];
    private int _loadedPageIndex;
    private bool _hasMoreHistory = true;
    private bool _isLoadingHistory;
    private ElementReference _messagesContainer;

    protected override Task OnInitializedAsync()
    {
        groupChatService.OnMessageReceived += HandleMessageReceived;
        groupChatService.OnUserJoined += HandleUserJoined;
        groupChatService.OnUserLeft += HandleUserLeft;
        groupChatService.OnHistoryLoaded += HandleHistoryLoaded;
        groupChatService.OnError += HandleError;
        return Task.CompletedTask;
    }

    public async Task OpenAsync()
    {
        if (_isVisible)
        {
            return;
        }

        _isVisible = true;
        StateHasChanged();

        // 等待 DOM 更新
        await Task.Delay(50);

        // 滚动到底部
        await ScrollToBottomAsync();

        // 如果未连接，尝试连接
        if (!groupChatService.IsConnected)
        {
            var sessionId = await GetOrCreateSessionIdAsync();
            try
            {
                await groupChatService.StartAsync(sessionId);
                _currentUserId = groupChatService.CurrentUser?.Id ?? "";

                // 加载历史记录
                await LoadInitialHistoryAsync();

                dialogService.Toast("已连接到群组聊天", ToastType.Success);
            }
            catch (Exception ex)
            {
                dialogService.Toast($"连接失败: {ex.Message}", ToastType.Error);
                _isVisible = false;
            }
        }
        else
        {
            _currentUserId = groupChatService.CurrentUser?.Id ?? "";
        }

        StateHasChanged();
    }

    public Task CloseAsync()
    {
        _isVisible = false;
        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter" && !e.ShiftKey)
        {
            await SendMessageAsync();
        }
    }

    private void HandleInputChange(ChangeEventArgs e)
    {
        _inputMessage = e.Value?.ToString() ?? "";
        _showCommands = _inputMessage.StartsWith("/");
        StateHasChanged();
    }

    private void SelectCommand(string command)
    {
        _inputMessage = command;
        _showCommands = false;
        StateHasChanged();
    }

    private async Task SendMessageAsync()
    {
        var message = _inputMessage.Trim();
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        if (message.StartsWith("/"))
        {
            await HandleCommandAsync(message);
            return;
        }

        _isSending = true;
        StateHasChanged();

        try
        {
            await groupChatService.SendMessageAsync(message);
            _inputMessage = "";
            _showCommands = false;
        }
        catch (Exception ex)
        {
            dialogService.Toast($"发送失败: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isSending = false;
            StateHasChanged();
        }
    }

    private async Task HandleCommandAsync(string command)
    {
        if (command == "/canvas")
        {
            _inputMessage = "";
            _showCommands = false;
            StateHasChanged();

            // TODO: 实现画板功能
            dialogService.Toast("画板功能待实现");
        }
        else
        {
            // 未知命令，当做普通消息发送
            await SendMessageAsync();
        }
    }

    private void HandleMessageReceived(GroupChatMessage message)
    {
        _ = InvokeAsync(() =>
        {
            _messages.Add(message);
            StateHasChanged();
            _ = ScrollToBottomAsync();
        });
    }

    private void HandleUserJoined(UserInfo user)
    {
        _ = InvokeAsync(async () =>
        {
            AddSystemMessage($"{user.Name} 加入了群组");
            await Task.CompletedTask;
        });
    }

    private void HandleUserLeft(UserInfo user)
    {
        _ = InvokeAsync(async () =>
        {
            AddSystemMessage($"{user.Name} 离开了群组");
            await Task.CompletedTask;
        });
    }

    private void HandleHistoryLoaded(List<GroupChatMessage> messages, bool hasMore)
    {
        _ = InvokeAsync(() =>
        {
            _hasMoreHistory = hasMore;
            _loadedPageIndex++;
            _isLoadingHistory = false;

            // 将新消息插入到列表顶部
            var newMessages = messages.ToList();
            newMessages.Reverse();
            _messages.InsertRange(0, newMessages);

            // 如果是第一次加载，滚动到底部
            if (_loadedPageIndex == 2)
            {
                _ = ScrollToBottomAsync();
            }

            StateHasChanged();
        });
    }

    private void HandleError(string error)
    {
        _ = InvokeAsync(() =>
        {
            dialogService.Toast(error, ToastType.Error);
        });
    }

    private void AddSystemMessage(string text)
    {
        _messages.Add(
            new GroupChatMessage
            {
                UserId = "",
                UserName = "系统",
                Avatar = "",
                Message = text,
                SendTime = DateTime.Now,
            }
        );
        StateHasChanged();
        _ = ScrollToBottomAsync();
    }

    private async Task LoadInitialHistoryAsync()
    {
        _loadedPageIndex = 1;
        _hasMoreHistory = true;
        _isLoadingHistory = true;
        StateHasChanged();

        await groupChatService.GetHistoryAsync(1, 50);
    }

    private async Task LoadMoreHistoryAsync()
    {
        if (_isLoadingHistory || !_hasMoreHistory)
        {
            return;
        }

        _isLoadingHistory = true;
        StateHasChanged();

        await groupChatService.GetHistoryAsync(_loadedPageIndex + 1, 50);
    }

    private async Task ScrollToBottomAsync()
    {
        try
        {
            await jsRuntime.InvokeVoidAsync(
                "eval",
                $@"(function() {{
                    const el = document.querySelector('.group-chat__messages');
                    if (el) {{
                        setTimeout(() => {{
                            el.scrollTop = el.scrollHeight;
                        }}, 0);
                    }}
                }})();"
            );
        }
        catch
        {
            // ignore
        }
    }

    private async Task<string> GetOrCreateSessionIdAsync()
    {
        try
        {
            var sessionId = await jsRuntime.InvokeAsync<string>(
                "localStorage.getItem",
                "groupChatSessionId"
            );
            if (string.IsNullOrWhiteSpace(sessionId))
            {
                sessionId = Guid.NewGuid().ToString();
                await jsRuntime.InvokeVoidAsync(
                    "localStorage.setItem",
                    "groupChatSessionId",
                    sessionId
                );
            }
            return sessionId;
        }
        catch
        {
            // 如果 localStorage 不可用，生成一个临时 ID
            return Guid.NewGuid().ToString();
        }
    }

    public void Dispose()
    {
        groupChatService.OnMessageReceived -= HandleMessageReceived;
        groupChatService.OnUserJoined -= HandleUserJoined;
        groupChatService.OnUserLeft -= HandleUserLeft;
        groupChatService.OnHistoryLoaded -= HandleHistoryLoaded;
        groupChatService.OnError -= HandleError;
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await Task.CompletedTask;
    }
}
