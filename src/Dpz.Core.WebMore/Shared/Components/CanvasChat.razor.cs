using System;
using System.Text.Json;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Shared.Components;

public partial class CanvasChat(
    IGroupChatService groupChatService,
    IJSRuntime jsRuntime,
    IAppDialogService dialogService
) : ComponentBase, IDisposable, IAsyncDisposable
{
    private bool _isVisible;
    private string _statusText = "等待开始...";
    private string _statusColor = "#666";
    private UserInfo? _drawingUser;
    private string? _currentUserId;
    private bool _isDrawing;
    private (double X, double Y) _lastPos;
    private string _color = "#000000";
    private int _brushSize = 2;
    private int _canvasWidth = 800;
    private int _canvasHeight = 600;
    private bool _canDraw;
    private bool _isAutoOpened; // 标记是否是自动打开的
    private IJSObjectReference? _jsModule;
    private DotNetObjectReference<CanvasChat>? _dotNetRef;

    protected override void OnInitialized()
    {
        groupChatService.OnDrawingUserChanged += HandleDrawingUserChanged;
        groupChatService.OnDraw += HandleDraw;
        _currentUserId = groupChatService.CurrentUser?.Id;
    }

    public async Task OpenAsync(bool autoOpen = false)
    {
        if (_isVisible)
        {
            return;
        }

        _isAutoOpened = autoOpen;
        _isVisible = true;
        _currentUserId = groupChatService.CurrentUser?.Id;
        StateHasChanged();

        // 等待 DOM 更新
        await Task.Delay(100);

        // 导入 JS 模块
        _jsModule ??= await jsRuntime.InvokeAsync<IJSObjectReference>(
            "import",
            "./Shared/Components/CanvasChat.razor.js"
        );

        // 初始化画笔颜色（从 localStorage 读取或根据主题设置）
        await InitializeColorAsync();

        // 初始化画布
        await InitializeCanvasAsync();

        // 设置窗口大小变化监听
        if (_dotNetRef == null)
        {
            _dotNetRef = DotNetObjectReference.Create(this);
            await _jsModule.InvokeVoidAsync("setupResizeListener", _dotNetRef);
        }

        // 只有手动打开时才显示 Toast
        if (!autoOpen)
        {
            dialogService.Toast("画板已打开");
        }
    }

    public Task CloseAsync()
    {
        _isVisible = false;

        // 如果是当前用户在画，释放权限
        if (_canDraw)
        {
            _ = ReleaseAccessAsync();
        }

        StateHasChanged();
        return Task.CompletedTask;
    }

    private async Task InitializeColorAsync()
    {
        try
        {
            if (_jsModule != null)
            {
                _color = await _jsModule.InvokeAsync<string>("getDefaultColor");
                StateHasChanged();
            }
        }
        catch
        {
            // 如果失败，使用默认值（黑色）
        }
    }

    private async Task SaveColorAsync()
    {
        try
        {
            if (_jsModule != null)
            {
                await _jsModule.InvokeVoidAsync("saveColor", _color);
            }
        }
        catch
        {
            // ignore
        }
    }

    private async Task InitializeCanvasAsync()
    {
        try
        {
            // 调整画布大小
            await ResizeCanvasAsync();

            // 设置画布上下文属性
            if (_jsModule != null)
            {
                await _jsModule.InvokeVoidAsync("initialize");
            }
        }
        catch
        {
            // ignore
        }
    }

    private async Task ResizeCanvasAsync()
    {
        try
        {
            if (_jsModule == null)
            {
                return;
            }

            var size = await _jsModule.InvokeAsync<ViewportSize>("getViewportSize");
            _canvasWidth = size.Width;
            _canvasHeight = size.Height;
            StateHasChanged();
        }
        catch
        {
            // ignore
        }
    }

    private async Task RequestAccessAsync()
    {
        try
        {
            await groupChatService.RequestDrawingAccessAsync();
            dialogService.Toast("正在请求画图权限...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"请求画图权限失败: {ex.Message}");
            dialogService.Toast($"请求失败: {ex.Message}", ToastType.Error);
        }
    }

    private async Task ReleaseAccessAsync()
    {
        try
        {
            await groupChatService.ReleaseDrawingAccessAsync();
            dialogService.Toast("已停止画图");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"释放画图权限失败: {ex.Message}");
            dialogService.Toast($"释放失败: {ex.Message}", ToastType.Error);
        }
    }

    private void HandleDrawingUserChanged(UserInfo? user)
    {
        _ = InvokeAsync(async () =>
        {
            _drawingUser = user;

            if (user != null)
            {
                if (user.Id == _currentUserId)
                {
                    _statusText = "正在作画：你自己";
                    _statusColor = "#4CAF50";
                    _canDraw = true;
                    dialogService.Toast("已获得画图权限", ToastType.Success);
                }
                else
                {
                    _statusText = $"正在作画：{user.Name}";
                    _statusColor = "#2196F3";
                    _canDraw = false;

                    // 别人开始画图时，自动打开画板（只读模式）
                    if (!_isVisible)
                    {
                        await OpenAsync(autoOpen: true);
                    }
                }
            }
            else
            {
                _statusText = "画板空闲";
                _statusColor = "#666";
                _canDraw = false;

                // 如果是自动打开的画板，当画图用户退出时自动关闭
                if (_isVisible && _isAutoOpened)
                {
                    _isVisible = false;
                }
            }

            StateHasChanged();
        });
    }

    private void HandleDraw(object data)
    {
        _ = InvokeAsync(async () =>
        {
            if (!_isVisible)
            {
                return;
            }

            try
            {
                var jsonElement = (JsonElement)data;
                var type = jsonElement.GetProperty("type").GetString();

                if (type == "clear")
                {
                    await ClearCanvasInternalAsync();
                }
                else if (type == "path")
                {
                    var x0 = jsonElement.GetProperty("x0").GetDouble() * _canvasWidth;
                    var y0 = jsonElement.GetProperty("y0").GetDouble() * _canvasHeight;
                    var x1 = jsonElement.GetProperty("x1").GetDouble() * _canvasWidth;
                    var y1 = jsonElement.GetProperty("y1").GetDouble() * _canvasHeight;
                    var color = jsonElement.GetProperty("color").GetString();
                    var size = jsonElement.GetProperty("size").GetInt32();

                    await DrawLineAsync(x0, y0, x1, y1, color!, size);
                }
            }
            catch
            {
                // ignore
            }
        });
    }

    private async Task OnColorChangedAsync()
    {
        await SaveColorAsync();
    }

    private async Task OnMouseDown(MouseEventArgs e)
    {
        if (!_canDraw)
        {
            return;
        }

        _isDrawing = true;
        var pos = await GetMousePositionAsync(e);
        _lastPos = pos;

        // 画一个点
        await DrawLineAsync(_lastPos.X, _lastPos.Y, pos.X, pos.Y, _color, _brushSize);
        await SendDrawDataAsync(
            new
            {
                type = "path",
                x0 = _lastPos.X / _canvasWidth,
                y0 = _lastPos.Y / _canvasHeight,
                x1 = pos.X / _canvasWidth,
                y1 = pos.Y / _canvasHeight,
                color = _color,
                size = _brushSize,
            }
        );
    }

    private async Task OnMouseMove(MouseEventArgs e)
    {
        if (!_isDrawing || !_canDraw)
        {
            return;
        }

        var pos = await GetMousePositionAsync(e);

        await DrawLineAsync(_lastPos.X, _lastPos.Y, pos.X, pos.Y, _color, _brushSize);
        await SendDrawDataAsync(
            new
            {
                type = "path",
                x0 = _lastPos.X / _canvasWidth,
                y0 = _lastPos.Y / _canvasHeight,
                x1 = pos.X / _canvasWidth,
                y1 = pos.Y / _canvasHeight,
                color = _color,
                size = _brushSize,
            }
        );

        _lastPos = pos;
    }

    private void OnMouseUp(MouseEventArgs e)
    {
        _isDrawing = false;
    }

    private async Task OnTouchStart(TouchEventArgs e)
    {
        if (!_canDraw || e.Touches.Length != 1)
        {
            return;
        }

        _isDrawing = true;
        var pos = await GetTouchPositionAsync(e.Touches[0]);
        _lastPos = pos;

        await DrawLineAsync(_lastPos.X, _lastPos.Y, pos.X, pos.Y, _color, _brushSize);
        await SendDrawDataAsync(
            new
            {
                type = "path",
                x0 = _lastPos.X / _canvasWidth,
                y0 = _lastPos.Y / _canvasHeight,
                x1 = pos.X / _canvasWidth,
                y1 = pos.Y / _canvasHeight,
                color = _color,
                size = _brushSize,
            }
        );
    }

    private async Task OnTouchMove(TouchEventArgs e)
    {
        if (!_isDrawing || !_canDraw || e.Touches.Length != 1)
        {
            return;
        }

        var pos = await GetTouchPositionAsync(e.Touches[0]);

        await DrawLineAsync(_lastPos.X, _lastPos.Y, pos.X, pos.Y, _color, _brushSize);
        await SendDrawDataAsync(
            new
            {
                type = "path",
                x0 = _lastPos.X / _canvasWidth,
                y0 = _lastPos.Y / _canvasHeight,
                x1 = pos.X / _canvasWidth,
                y1 = pos.Y / _canvasHeight,
                color = _color,
                size = _brushSize,
            }
        );

        _lastPos = pos;
    }

    private void OnTouchEnd(TouchEventArgs e)
    {
        _isDrawing = false;
    }

    private async Task<(double X, double Y)> GetMousePositionAsync(MouseEventArgs e)
    {
        try
        {
            if (_jsModule == null)
            {
                return (e.OffsetX, e.OffsetY);
            }

            var position = await _jsModule.InvokeAsync<CanvasPosition>(
                "getCanvasPosition",
                e.ClientX,
                e.ClientY
            );
            return (position.X, position.Y);
        }
        catch
        {
            return (e.OffsetX, e.OffsetY);
        }
    }

    private async Task<(double X, double Y)> GetTouchPositionAsync(TouchPoint touch)
    {
        try
        {
            if (_jsModule == null)
            {
                return (0, 0);
            }

            var position = await _jsModule.InvokeAsync<CanvasPosition>(
                "getCanvasPosition",
                touch.ClientX,
                touch.ClientY
            );
            return (position.X, position.Y);
        }
        catch
        {
            return (0, 0);
        }
    }

    private async Task DrawLineAsync(
        double x0,
        double y0,
        double x1,
        double y1,
        string color,
        int size
    )
    {
        try
        {
            if (_jsModule != null)
            {
                await _jsModule.InvokeVoidAsync("drawLine", x0, y0, x1, y1, color, size);
            }
        }
        catch
        {
            // ignore
        }
    }

    private async Task ClearCanvasAsync()
    {
        if (!_canDraw)
        {
            return;
        }

        await ClearCanvasInternalAsync();
        await SendDrawDataAsync(new { type = "clear" });
    }

    private async Task ClearCanvasInternalAsync()
    {
        try
        {
            if (_jsModule != null)
            {
                await _jsModule.InvokeVoidAsync("clearCanvas");
            }
        }
        catch
        {
            // ignore
        }
    }

    private async Task SendDrawDataAsync(object data)
    {
        try
        {
            await groupChatService.SendDrawingDataAsync(data);
        }
        catch
        {
            // ignore
        }
    }

    [JSInvokable]
    public async Task OnWindowResized()
    {
        if (_isVisible)
        {
            await ResizeCanvasAsync();
        }
    }

    public void Dispose()
    {
        groupChatService.OnDrawingUserChanged -= HandleDrawingUserChanged;
        groupChatService.OnDraw -= HandleDraw;
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();

        if (_jsModule != null)
        {
            try
            {
                await _jsModule.InvokeVoidAsync("cleanupResizeListener");
            }
            catch
            {
                // ignore
            }

            await _jsModule.DisposeAsync();
        }

        _dotNetRef?.Dispose();
    }

    // 辅助类型
    private class ViewportSize
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    private class CanvasPosition
    {
        public double X { get; set; }
        public double Y { get; set; }
    }
}
