using System.Threading;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Shared.Components.Dialog;

public partial class ToastBox
{
    [Parameter]
    public ToastModel Model { get; set; } = new();

    [Parameter]
    public EventCallback<ToastModel> OnClose { get; set; }

    private bool _isLeaving;
    private Timer? _timer;

    protected override void OnInitialized()
    {
        _timer = new Timer(
            async _ =>
            {
                await StartClose();
            },
            null,
            Model.Duration,
            Timeout.Infinite
        );
    }

    private string GetToastClass() => Model.Type.ToString().ToLower();

    private string GetIconClass() =>
        Model.Type switch
        {
            ToastType.Success => "fa-check-circle",
            ToastType.Warning => "fa-exclamation-triangle",
            ToastType.Error => "fa-times-circle",
            _ => "fa-info-circle",
        };

    private async Task StartClose()
    {
        await InvokeAsync(async () =>
        {
            _isLeaving = true;
            StateHasChanged();

            // Wait for animation (300ms from CSS)
            await Task.Delay(300);

            await OnClose.InvokeAsync(Model);
        });
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
