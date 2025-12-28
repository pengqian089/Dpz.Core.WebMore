using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Shared.Components.Dialog;

public partial class DialogContainer(IAppDialogService dialogService, IJSRuntime jsRuntime)
{
    private readonly List<DialogModel> _dialogs = [];
    private readonly List<ToastModel> _toasts = [];
    private readonly List<NotificationModel> _notifications = [];

    protected override void OnInitialized()
    {
        dialogService.OnDialogShow += ShowDialog;
        dialogService.OnToastShow += ShowToast;
        dialogService.OnNotificationShow += ShowNotification;
        dialogService.OnCloseAllNotifications += CloseAllNotifications;
    }

    private async void ShowDialog(DialogModel model)
    {
        _dialogs.Add(model);
        await InvokeAsync(StateHasChanged);
        await jsRuntime.InvokeVoidAsync("dialogInterop.disableBodyScroll");
    }

    private async void RemoveDialog(DialogModel model)
    {
        _dialogs.Remove(model);
        await InvokeAsync(StateHasChanged);

        if (_dialogs.Count == 0)
        {
            await jsRuntime.InvokeVoidAsync("dialogInterop.enableBodyScroll");
        }
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
        dialogService.OnDialogShow -= ShowDialog;
        dialogService.OnToastShow -= ShowToast;
        dialogService.OnNotificationShow -= ShowNotification;
        dialogService.OnCloseAllNotifications -= CloseAllNotifications;
    }
}
