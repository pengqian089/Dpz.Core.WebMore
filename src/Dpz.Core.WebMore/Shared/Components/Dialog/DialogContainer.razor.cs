using System.Collections.Generic;
using System.Linq;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Shared.Components.Dialog;

public partial class DialogContainer
{
    [Inject]
    public IAppDialogService DialogService { get; set; } = default!;

    private readonly List<DialogModel> _dialogs = new();
    private readonly List<ToastModel> _toasts = new();
    private readonly List<NotificationModel> _notifications = new();

    protected override void OnInitialized()
    {
        DialogService.OnDialogShow += ShowDialog;
        DialogService.OnToastShow += ShowToast;
        DialogService.OnNotificationShow += ShowNotification;
        DialogService.OnCloseAllNotifications += CloseAllNotifications;
    }

    private void ShowDialog(DialogModel model)
    {
        _dialogs.Add(model);
        InvokeAsync(StateHasChanged);
    }

    private void RemoveDialog(DialogModel model)
    {
        _dialogs.Remove(model);
        InvokeAsync(StateHasChanged);
    }

    private void ShowToast(ToastModel model)
    {
        _toasts.Add(model);
        InvokeAsync(StateHasChanged);
    }

    private void RemoveToast(ToastModel model)
    {
        _toasts.Remove(model);
        InvokeAsync(StateHasChanged);
    }

    private void ShowNotification(NotificationModel model)
    {
        _notifications.Add(model);
        InvokeAsync(StateHasChanged);
    }

    private void RemoveNotification(NotificationModel model)
    {
        _notifications.Remove(model);
        InvokeAsync(StateHasChanged);
    }

    private void CloseAllNotifications()
    {
        // This is tricky because we want animation.
        // We can call Close on all models.
        foreach (var n in _notifications.ToList())
        {
            n.Close?.Invoke();
        }
    }

    public void Dispose()
    {
        DialogService.OnDialogShow -= ShowDialog;
        DialogService.OnToastShow -= ShowToast;
        DialogService.OnNotificationShow -= ShowNotification;
        DialogService.OnCloseAllNotifications -= CloseAllNotifications;
    }
}
