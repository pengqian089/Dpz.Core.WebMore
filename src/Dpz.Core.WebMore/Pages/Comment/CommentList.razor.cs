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
    public List<CommentModel> Comments { get; set; }
    
    [Parameter]
    public int CurrentPageIndex { get; set; }
    
    [Parameter]
    public int TotalPageCount { get; set; }
    
    [Parameter]
    public ToNextPage ToNextPageAsync { get; set; }
    
    [Parameter]
    public Action OnReply { get; set; }
    
    [Parameter]
    public Action OnCancelReply { get; set; }
    
    [Parameter]
    public Func<SendComment,Task> SendCommentAsync { get; set; }

    [Inject]
    IJSRuntime JsRuntime { get; set; }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await JsRuntime.InvokeVoidAsync("Prism.highlightAll");
        await base.OnAfterRenderAsync(firstRender);
    }


    public delegate Task<(List<CommentModel> comments,int currentPageIndex,int totalPageCount)> ToNextPage(int nextPageIndex);
    
    

    public void Refresh(List<CommentModel> comments,int currentPageIndex,int totalPageCount)
    {
        CurrentPageIndex = currentPageIndex;
        TotalPageCount = totalPageCount;
        Comments = comments;
        StateHasChanged();
    }

    bool _loadNextPage = false;
    private async Task NextAsync()
    {
        if (ToNextPageAsync != null)
        {
            _loadNextPage = true;
            var (comments,currentPageIndex,totalPageCount) = await ToNextPageAsync(CurrentPageIndex + 1);
            Comments.AddRange(comments);
            CurrentPageIndex = currentPageIndex;
            TotalPageCount = totalPageCount;
            _loadNextPage = false;
            StateHasChanged();
        }
    }

    private void Reply(dynamic comment)
    {
        OnReply?.Invoke();
        foreach (var model in Comments)
        {
            model.ShowReply = false;
            foreach (var child in model.Children)
            {
                child.ShowReply = false;
            }
        }
        comment.ShowReply = true;
        //StateHasChanged();
    }
}