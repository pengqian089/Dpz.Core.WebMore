using System;
using System.Threading.Tasks;
using Dpz.Core.EnumLibrary;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages.Comment;

public partial class CommentForm(IJSRuntime jsRuntime, IAppDialogService appDialogService)
{
    [Parameter]
    [EditorRequired]
    public required CommentNode Node { get; set; }

    [Parameter]
    [EditorRequired]
    public required string Relation { get; set; }

    [Parameter]
    public string? ReplyId { get; set; }

    [Parameter]
    public Func<SendComment, Task<bool>>? SendCommentAsync { get; set; }

    [Parameter]
    public EventCallback OnCancelReply { get; set; }

    bool _isSending;

    private readonly SendComment _model = new();

    protected override async Task OnInitializedAsync()
    {
        _model.NickName = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "nickname");
        _model.Email = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "email");
        _model.Site = await jsRuntime.InvokeAsync<string>("localStorage.getItem", "site");
        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        _model.Node = Node;
        _model.Relation = Relation;
        _model.ReplyId = ReplyId;
        await base.OnParametersSetAsync();
    }

    private async Task SendAsync(EditContext arg)
    {
        if (!arg.Validate())
        {
            foreach (var message in arg.GetValidationMessages())
            {
                appDialogService.Toast(message, ToastType.Warning);
            }
            return;
        }

        _isSending = true;

        if (!string.IsNullOrEmpty(_model.NickName))
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", "nickname", _model.NickName);
        }
        if (!string.IsNullOrEmpty(_model.Email))
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", "email", _model.Email);
        }
        if (!string.IsNullOrEmpty(_model.Site))
        {
            await jsRuntime.InvokeVoidAsync("localStorage.setItem", "site", _model.Site);
        }

        StateHasChanged();
        var success = false;
        try
        {
            if (arg.Model is SendComment sendComment && SendCommentAsync != null)
            {
                success = await SendCommentAsync.Invoke(sendComment);
            }
        }
        catch (Exception ex)
        {
            appDialogService.Toast($"发送失败: {ex.Message}", ToastType.Error);
        }
        finally
        {
            _isSending = false;
            StateHasChanged();
        }

        if (success)
        {
            _model.CommentText = "";
        }
    }

    private async Task ShowPicture()
    {
        var url = await appDialogService.PromptAsync("请输入网络图片地址", "添加图片", "https://");
        if (!string.IsNullOrWhiteSpace(url))
        {
            if (!url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                await appDialogService.AlertAsync("只支持https协议的图片", "错误");
                return;
            }
            _model.CommentText += $"\r\n\r\n![]({url})";
        }
    }
}
