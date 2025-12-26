using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages.Comment;

public partial class CommentList
{
    [Parameter]
    public List<CommentModel> Comments { get; set; } = [];

    [Parameter]
    public int TotalPageCount { get; set; }

    [Parameter]
    public int CurrentPageIndex { get; set; }

    [Parameter]
    public Func<
        int,
        Task<(List<CommentModel> comments, int currentPageIndex, int totalPageCount)>
    >? ToNextPageAsync { get; set; }

    [Parameter]
    public Func<SendComment, Task>? SendCommentAsync { get; set; }

    [Parameter]
    public EventCallback<string> OnReply { get; set; }

    [Parameter]
    public EventCallback OnCancelReply { get; set; }

    [Parameter]
    public string? ReplyId { get; set; }

    bool _loadNextPage;

    public void Refresh(List<CommentModel> comments, int currentPageIndex, int totalPageCount)
    {
        Comments = comments;
        CurrentPageIndex = currentPageIndex;
        TotalPageCount = totalPageCount;
        StateHasChanged();
    }

    private async Task NextAsync()
    {
        _loadNextPage = true;
        var (comments, currentPageIndex, totalPageCount) = await ToNextPageAsync?.Invoke(
            CurrentPageIndex + 1
        )!;
        Comments.AddRange(comments);
        CurrentPageIndex = currentPageIndex;
        TotalPageCount = totalPageCount;
        _loadNextPage = false;
    }
}
