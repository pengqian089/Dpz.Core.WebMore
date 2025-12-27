using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dpz.Core.EnumLibrary;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Pages.Comment;

public partial class Comment(ICommentService commentService, IAppDialogService dialogService)
{
    [Parameter]
    [EditorRequired]
    public required CommentNode Node { get; set; }

    [Parameter]
    [EditorRequired]
    public required string Relation { get; set; }

    private IPagedList<CommentModel> _comments = PagedList<CommentModel>.Empty();

    private bool _loading = true;

    private string? _replyId;

    private int _pageIndex = 1;

    private const int PageSize = 10;

    private CommentList? _commentList;

    protected override async Task OnParametersSetAsync()
    {
        await LoadCommentsAsync();
        await base.OnParametersSetAsync();
    }

    private async Task LoadCommentsAsync()
    {
        _loading = true;
        if (string.IsNullOrEmpty(Relation))
        {
            _comments = PagedList<CommentModel>.Empty();
            _loading = false;
            return;
        }
        _comments = await commentService.GetPageAsync(Node, Relation, _pageIndex);
        _loading = false;
    }

    private async Task<bool> SendAsync(SendComment arg)
    {
        var result = await commentService.SendAsync(arg, PageSize);
        if (!result.Success || result.Data == null)
        {
            await dialogService.AlertAsync(result.Message);
            return false;
        }

        _comments = result.Data;
        ;
        if (_commentList != null)
        {
            _commentList.Refresh(
                _comments.ToList(),
                _comments.CurrentPageIndex,
                _comments.TotalPageCount
            );
        }
        CancelReply();
        return true;
    }

    private async Task<(
        List<CommentModel> comments,
        int currentPageIndex,
        int totalPageCount
    )> NextAsync(int nextPageIndex)
    {
        _pageIndex = nextPageIndex;
        var result = await commentService.GetPageAsync(Node, Relation, nextPageIndex);
        return (result.ToList(), result.CurrentPageIndex, result.TotalPageCount);
    }

    private async Task RefreshAsync()
    {
        _pageIndex = 1;
        await LoadCommentsAsync();
        if (_commentList != null)
        {
            _commentList.Refresh(
                _comments.ToList(),
                _comments.CurrentPageIndex,
                _comments.TotalPageCount
            );
        }
        CancelReply();
    }

    void HandleReply(string replyId)
    {
        _replyId = replyId;
    }

    void CancelReply()
    {
        _replyId = null;
    }
}
