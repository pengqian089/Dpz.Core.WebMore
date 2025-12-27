using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages;

public partial class Bookmark(
    IBookmarkService bookmarkService,
    NavigationManager navigationManager,
    IJSRuntime jsRuntime
) : ComponentBase, IAsyncDisposable
{
    [SupplyParameterFromQuery(Name = "title")]
    public string? Title { get; set; }

    [SupplyParameterFromQuery(Name = "categories")]
    public string[] SelectedCategories { get; set; } = [];

    private List<BookmarkModel> _source = [];

    private List<string> Categories =>
        _source.SelectMany(x => x.Categories).Distinct().OrderByDescending(x => x).ToList();

    private bool _loading;
    private IJSObjectReference? _module;
    private string _searchText = "";
    private List<string> _suggestions = [];
    private Timer? _debounceTimer;
    private int _selectedIndex = -1;

    protected override async Task OnInitializedAsync()
    {
        _searchText = Title ?? "";
        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        _searchText = Title ?? "";
        await LoadDataAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            _module = await jsRuntime.InvokeAsync<IJSObjectReference>(
                "import",
                "./Pages/Bookmark.razor.js"
            );
            await _module.InvokeVoidAsync("init", ".bookmark__grid");
        }
        // Always try to layout after render, the JS observer should handle it but we can force it if needed
        // await _module.InvokeVoidAsync("layout");
    }

    private async Task LoadDataAsync()
    {
        _loading = true;
        // Assuming GetBookmarksAsync takes List<string> for categories
        _source = await bookmarkService.GetBookmarksAsync(Title, SelectedCategories.ToList());
        _loading = false;
    }

    private void OnSearchInput(ChangeEventArgs e)
    {
        _searchText = e.Value?.ToString() ?? "";
        _selectedIndex = -1;
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(
            async _ =>
            {
                if (string.IsNullOrWhiteSpace(_searchText))
                {
                    _suggestions.Clear();
                    await InvokeAsync(StateHasChanged);
                }
                else
                {
                    await InvokeAsync(async () =>
                    {
                        var results = await bookmarkService.SearchAsync(
                            _searchText,
                            SelectedCategories.ToList()
                        );
                        _suggestions = results.ToList();
                        StateHasChanged();
                    });
                }
            },
            null,
            300,
            Timeout.Infinite
        );
    }

    private async Task OnKeyDown(KeyboardEventArgs e)
    {
        if (_suggestions.Count == 0)
        {
            return;
        }

        if (e.Key == "ArrowDown")
        {
            _selectedIndex = (_selectedIndex + 1) % _suggestions.Count;
            _searchText = _suggestions[_selectedIndex];
            if (_module != null)
            {
                await _module.InvokeVoidAsync("scrollToItem", _selectedIndex);
            }
        }
        else if (e.Key == "ArrowUp")
        {
            if (_selectedIndex == -1)
            {
                _selectedIndex = 0;
            }
            _selectedIndex = (_selectedIndex - 1 + _suggestions.Count) % _suggestions.Count;
            _searchText = _suggestions[_selectedIndex];
            if (_module != null)
            {
                await _module.InvokeVoidAsync("scrollToItem", _selectedIndex);
            }
        }
        else if (e.Key == "Enter")
        {
            NavigateToSearch();
        }
        else if (e.Key == "Escape")
        {
            _suggestions.Clear();
            _selectedIndex = -1;
        }
    }

    private void NavigateToSearch()
    {
        var query = new Dictionary<string, object?>
        {
            ["title"] = _searchText,
            ["categories"] = SelectedCategories,
        };
        var uri = navigationManager.GetUriWithQueryParameters("/bookmark", query);
        navigationManager.NavigateTo(uri);
        _suggestions.Clear();
    }

    private void SelectSuggestion(string suggestion)
    {
        _searchText = suggestion;
        NavigateToSearch();
    }

    private string GetCategoryUrl(string category)
    {
        var list = SelectedCategories.ToList();
        if (list.Contains(category))
        {
            list.Remove(category);
        }
        else
        {
            list.Add(category);
        }

        var query = new Dictionary<string, object?> { ["categories"] = list.ToArray() };

        if (!string.IsNullOrEmpty(Title))
        {
            query["title"] = Title;
        }

        return navigationManager.GetUriWithQueryParameters("/bookmark", query);
    }

    public async ValueTask DisposeAsync()
    {
        if (_debounceTimer != null)
        {
            await _debounceTimer.DisposeAsync();
        }
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }
    }
}
