using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Shared.Components.Dialog;

public partial class DialogContainer(IAppDialogService dialogService, IJSRuntime jsRuntime)
    : IAsyncDisposable
{
    private readonly List<DialogModel> _dialogs = [];
    private readonly List<ToastModel> _toasts = [];
    private readonly List<NotificationModel> _notifications = [];
    private IJSObjectReference? _dialogModule;

    protected override void OnInitialized()
    {
        dialogService.OnDialogShow += ShowDialog;
        dialogService.OnToastShow += ShowToast;
        dialogService.OnNotificationShow += ShowNotification;
        dialogService.OnCloseAllNotifications += CloseAllNotifications;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _dialogModule = await jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./js/modules/dialog-interop.js"
            );
        }
    }

    private async void ShowDialog(DialogModel model)
    {
        _dialogs.Add(model);
        await InvokeAsync(StateHasChanged);

        if (_dialogModule != null)
        {
            try
            {
                await _dialogModule.InvokeVoidAsync("disableBodyScroll", model.DisableBodyScroll);
            }
            catch
            {
                // Fallback if module not loaded
            }
        }
    }

    private async void RemoveDialog(DialogModel model)
    {
        _dialogs.Remove(model);
        await InvokeAsync(StateHasChanged);

        if (_dialogs.Count == 0 && _dialogModule != null)
        {
            try
            {
                await _dialogModule.InvokeVoidAsync("enableBodyScroll");
            }
            catch
            {
                // Ignore if module disposed
            }
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

    public async ValueTask DisposeAsync()
    {
        dialogService.OnDialogShow -= ShowDialog;
        dialogService.OnToastShow -= ShowToast;
        dialogService.OnNotificationShow -= ShowNotification;
        dialogService.OnCloseAllNotifications -= CloseAllNotifications;

        if (_dialogModule != null)
        {
            try
            {
                await _dialogModule.DisposeAsync();
            }
            catch
            {
                // Ignore disposal errors
            }
        }
    }
}
