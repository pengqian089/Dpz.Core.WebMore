using System.Threading.Tasks;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace Dpz.Core.WebMore.Shared.Components.Dialog;

public partial class DialogBox
{
    [Parameter]
    public DialogModel Model { get; set; } = new();

    [Parameter]
    public EventCallback<DialogModel> OnClose { get; set; }

    private bool _isVisible;
    private string _inputValue = "";
    private ElementReference _inputRef;
    private ElementReference _confirmBtnRef;

    protected override async Task OnInitializedAsync()
    {
        _inputValue = Model.DefaultValue;
        // Trigger animation
        await Task.Delay(10);
        _isVisible = true;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            if (Model.Type == DialogType.Prompt)
            {
                await _inputRef.FocusAsync();
            }
            else if (Model.Type != DialogType.Component)
            {
                await _confirmBtnRef.FocusAsync();
            }
        }
    }

    private void HandleKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            Close(true);
        }
        else if (e.Key == "Escape")
        {
            Close(null);
        }
    }

    private async void Close(object? result)
    {
        _isVisible = false;
        StateHasChanged();

        // Wait for animation
        await Task.Delay(300);

        if (Model.Type == DialogType.Prompt && result is true)
        {
            Model.TaskSource.TrySetResult(_inputValue);
        }
        else if (Model.Type == DialogType.Confirm)
        {
            Model.TaskSource.TrySetResult(result is true);
        }
        else
        {
            Model.TaskSource.TrySetResult(result);
        }

        await OnClose.InvokeAsync(Model);
    }

    public void Dispose()
    {
        
    }
}
