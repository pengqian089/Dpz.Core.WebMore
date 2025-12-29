using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Service;

public interface IAppDialogService
{
    Task AlertAsync(string message, string title = "提示");
    Task<bool> ConfirmAsync(string message, string title = "确认");

    Task<string?> PromptAsync(string message, string title = "输入", string defaultValue = "");

    Task<TResult?> ShowComponentAsync<TResult>(
        string title,
        RenderFragment childContent,
        string width = "",
        bool disableBodyScroll = true
    );

    Task ShowComponentAsync(
        string title,
        RenderFragment childContent,
        string width = "",
        bool disableBodyScroll = true
    );

    void Toast(string message, ToastType type = ToastType.Info, int duration = 3000);

    // Notification methods
    NotificationModel ShowNotification(
        string content,
        string title = "",
        NotificationType type = NotificationType.Info,
        int autoClose = 0
    );

    NotificationModel ShowNotification(NotificationOptions options);
    void CloseAllNotifications();

    // Events for UI components
    event Action<DialogModel> OnDialogShow;
    event Action<ToastModel> OnToastShow;
    event Action<NotificationModel> OnNotificationShow;
    event Action OnCloseAllNotifications;
}

public enum ToastType
{
    Info,
    Success,
    Warning,
    Error,
}

public enum NotificationType
{
    Info,
    Success,
    Warning,
    Error,
}

public class NotificationOptions
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public double[] Bars { get; set; } = [];
    public int AutoClose { get; set; } = 0;
    public NotificationType Type { get; set; } = NotificationType.Info;
}

public class DialogModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public DialogType Type { get; set; }
    public string DefaultValue { get; set; } = "";
    public RenderFragment? Content { get; set; }
    public string Width { get; set; } = "";

    /// <summary>
    /// 是否禁用滚动条
    /// 默认禁用滚动
    /// </summary>
    public bool DisableBodyScroll { get; set; } = true;
    public TaskCompletionSource<object?> TaskSource { get; set; } = new();
}

public enum DialogType
{
    Alert,
    Confirm,
    Prompt,
    Component,
}

public class ToastModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Message { get; set; } = "";
    public ToastType Type { get; set; }
    public int Duration { get; set; }
}

public class NotificationModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public NotificationOptions Options { get; set; } = new();

    // Methods to update state dynamically if needed
    public Action<string>? UpdateContent { get; set; }
    public Action<string>? UpdateTitle { get; set; }
    public Action<double[]>? UpdateProgress { get; set; }
    public Action<NotificationType>? UpdateType { get; set; }
    public Action? Close { get; set; }
}
