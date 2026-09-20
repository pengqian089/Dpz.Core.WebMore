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
    private CanvasChat? _canvasChat;

    private const int MobileBreakpoint = 768;
    private const string ChatHash = "#group-chat";
    private bool _isMobile;
    private IJSObjectReference? _jsModule;

    protected override async Task OnInitializedAsync()
    {
        groupChatService.OnMessageReceived += HandleMessageReceived;
        groupChatService.OnUserJoined += HandleUserJoined;
        groupChatService.OnUserLeft += HandleUserLeft;
        groupChatService.OnHistoryLoaded += HandleHistoryLoaded;
        groupChatService.OnError += HandleError;

        // 导入 JS 模块
        _jsModule = await jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./Shared/Components/GroupChat.razor.js"
        );

        // 检测是否为移动端
        _isMobile = await IsMobileAsync();

        // 监听 hash 变化（移动端返回按钮）
        await InitHashListenerAsync();
    }

    public async Task OpenAsync()
    {
        if (_isVisible)
        {
            return;
        }

        _isVisible = true;

        // 移动端：添加 hash 和禁用滚动
        if (_isMobile)
        {
            await AddHashAsync();
            await DisableBodyScrollAsync();
        }

        StateHasChanged();

        // 等待 DOM 更新
        await Task.Delay(50);

        // 阻止输入框 Enter 键默认行为
        if (_jsModule != null)
        {
            await _jsModule.InvokeVoidAsync("preventEnterKey");
        }

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

    public async Task CloseAsync()
    {
        _isVisible = false;

        // 移动端：清除 hash 和恢复滚动
        if (_isMobile)
        {
            await RemoveHashAsync();
            await EnableBodyScrollAsync();
        }

        StateHasChanged();
        await Task.CompletedTask;
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

            // 打开画板
            if (_canvasChat != null)
            {
                await _canvasChat.OpenAsync();
            }
        }
        else
        {
            // 未知命令，当作普通消息发送
            _showCommands = false;
            _isSending = true;
            StateHasChanged();

            try
            {
                await groupChatService.SendMessageAsync(command);
                _inputMessage = "";
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

        await groupChatService.GetHistoryAsync();
    }

    private async Task LoadMoreHistoryAsync()
    {
        if (_isLoadingHistory || !_hasMoreHistory)
        {
            return;
        }

        _isLoadingHistory = true;
        StateHasChanged();

        await groupChatService.GetHistoryAsync(_loadedPageIndex + 1);
    }

    private async Task ScrollToBottomAsync()
    {
        try
        {
            if (_jsModule != null)
            {
                await _jsModule.InvokeVoidAsync("scrollToBottom");
            }
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

    private async Task<bool> IsMobileAsync()
    {
        try
        {
            if (_jsModule == null)
            {
                return false;
            }

            return await _jsModule.InvokeAsync<bool>("isMobile", MobileBreakpoint);
        }
        catch
        {
            return false;
        }
    }

    private async Task InitHashListenerAsync()
    {
        // Blazor WASM 中 hash 监听的处理比较复杂
        // 简化方案：当用户点击关闭或主动关闭时清除 hash
        // 如果需要更复杂的返回按钮处理，可以使用 NavigationManager
        await Task.CompletedTask;
    }

    private async Task AddHashAsync()
    {
        try
        {
            if (_jsModule != null)
            {
                await _jsModule.InvokeVoidAsync("addHash", ChatHash);
            }
        }
        catch
        {
            // ignore
        }
    }

    private async Task RemoveHashAsync()
    {
        try
        {
            if (_jsModule != null)
            {
                await _jsModule.InvokeVoidAsync("removeHash", ChatHash);
            }
        }
        catch
        {
            // ignore
        }
    }

    private async Task DisableBodyScrollAsync()
    {
        try
        {
            if (_jsModule != null)
            {
                await _jsModule.InvokeVoidAsync("disableBodyScroll");
            }
        }
        catch
        {
            // ignore
        }
    }

    private async Task EnableBodyScrollAsync()
    {
        try
        {
            if (_jsModule != null)
            {
                await _jsModule.InvokeVoidAsync("enableBodyScroll");
            }
        }
        catch
        {
            // ignore
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

        if (_jsModule != null)
        {
            await _jsModule.DisposeAsync();
        }
    }
}
