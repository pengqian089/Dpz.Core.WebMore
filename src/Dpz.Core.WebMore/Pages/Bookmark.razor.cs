using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages;

public partial class Bookmark
{
    [Inject] private IBookmarkService BookmarkService { get; set; }
    
    [Inject] private IJSRuntime JsRuntime { get; set; }

    private string _title = null;

    private readonly List<string> _categories = [];

    private List<BookmarkModel> _source = [];

    private bool _loading = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadDataAsync();
        await base.OnInitializedAsync();
    }

    private async Task LoadDataAsync()
    {
        _loading = true;

        _source = await BookmarkService.GetBookmarksAsync(_title, _categories);

        _loading = false;
    }
    
    

    private async Task<IEnumerable<string>> SearchAsync(string value,CancellationToken token)
    {
        if (string.IsNullOrEmpty(value))
        {
            return [];
        }

        return await BookmarkService.SearchAsync(value, _categories);
    }
    
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        Console.WriteLine("on after render async");
        await JsRuntime.InvokeVoidAsync("showLazyImage");
        await base.OnAfterRenderAsync(firstRender);
    }

    private async Task SelectCategoryAsync(string category)
    {
        if (_categories.Contains(category))
        {
            return;
        }
        _categories.Add(category);
        await LoadDataAsync();
    }

    private async Task UnSelectCategoryAsync(string category)
    {
        if (!_categories.Contains(category))
        {
            return;
        }

        _categories.Remove(category);
        await LoadDataAsync();
    }
}