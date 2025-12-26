using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Dpz.Core.EnumLibrary;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Pages.CodeComponent;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages;

public partial class CodeView(
    ICodeService codeService,
    NavigationManager navigationManager,
    IAppDialogService dialogService,
    ILogger<CodeView> logger,
    IJSRuntime jsRuntime
) : IAsyncDisposable
{
    [SupplyParameterFromQuery]
    [Parameter]
    public string? Path { get; set; }

    private CodeNoteTree _treeData = new();
    private CodeNoteTree? _selectedNode;
    private CodeNoteTree? _currentFolderNode;

    private bool _isLoading;
    private bool _isRestoringPath;

    private IEnumerable<string> _currentPath = [];

    private ElementReference _sidebarElement;
    private ElementReference _resizerElement;
    private IJSObjectReference? _module;
    private Tree _treeComponent = new();

    protected override async Task OnInitializedAsync()
    {
        await LoadTreeData(null);
        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (firstRender)
        {
            try
            {
                _module = await jsRuntime.InvokeAsync<IJSObjectReference>(
                    "import",
                    "./Pages/CodeView.razor.js"
                );
                await _module.InvokeVoidAsync(
                    "initResizableSidebar",
                    _resizerElement,
                    _sidebarElement
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to initialize sidebar resizer");
            }
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    protected override async Task OnParametersSetAsync()
    {
        var path = string.Join("/", _currentPath);
        if (
            !string.IsNullOrEmpty(Path)
            && !string.Equals(path, Path, StringComparison.InvariantCulture)
        )
        {
            var paths = Path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // If paths match current selected node, do nothing
            if (_selectedNode != null && _selectedNode.CurrentPaths.SequenceEqual(paths))
            {
                return;
            }
            // If paths match current folder node, do nothing
            if (
                _selectedNode == null
                && _currentFolderNode != null
                && _currentFolderNode.CurrentPaths.SequenceEqual(paths)
            )
            {
                return;
            }

            // Start restoration process
            _isRestoringPath = true;
            try
            {
                var tempPaths = new List<string>();

                // 等待根目录加载完成
                if (_treeData.Type == FileSystemType.NoExists || _isLoading)
                {
                    await Task.Delay(100);
                    _isRestoringPath = false;
                    await OnParametersSetAsync();
                    return;
                }

                foreach (var item in paths)
                {
                    tempPaths.Add(item);
                    // 找到当前层级的节点
                    var node = _treeComponent.FindNodeByPath(tempPaths);
                    if (node == null)
                    {
                        var parentPaths = tempPaths.Take(tempPaths.Count - 1).ToList();
                        var parentNode = _treeComponent.FindNodeByPath(parentPaths);
                        if (parentNode != null)
                        {
                            await parentNode.ExpandNodeAsync();
                            node = _treeComponent.FindNodeByPath(tempPaths);
                        }
                    }

                    if (node != null)
                    {
                        // Always expand/select to ensure highlighting
                        await node.ExpandNodeAsync();
                    }
                    StateHasChanged();
                }

                // Scroll to active element after restoration
                if (_module != null)
                {
                    await _module.InvokeVoidAsync("scrollToActive", _sidebarElement);
                }
            }
            finally
            {
                _isRestoringPath = false;
            }
        }
        await base.OnParametersSetAsync();
    }

    private string? _homeReadmeContent;

    private async Task LoadTreeData(IEnumerable<string>? path)
    {
        _isLoading = true;
        path ??= [];
        var currentPath = path.ToArray();
        _treeData = await codeService.GetTreeAsync(currentPath);
        _homeReadmeContent = _treeData.ReadmeContent;
        _currentPath = currentPath;
        _currentFolderNode = _treeData; // Default to root
        _isLoading = false;
    }

    private string? _search;
    private string? _tempSearch;

    private async Task SearchAsync()
    {
        if (string.IsNullOrEmpty(_search) && string.IsNullOrEmpty(_tempSearch))
        {
            dialogService.Toast("请输入搜索关键字");
            return;
        }

        if (_search == _tempSearch)
        {
            dialogService.Toast("搜索关键字未改变");
            return;
        }

        _isLoading = true;
        try
        {
            if (string.IsNullOrEmpty(_search))
            {
                await HandleEmptySearch();
            }
            else
            {
                await HandleSearchWithKeyword();
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    private async Task HandleEmptySearch()
    {
        _tempSearch = null;
        _treeData = await codeService.GetTreeAsync(null);
        _selectedNode = null;
        _currentFolderNode = _treeData;
        navigationManager.NavigateTo("/code");
    }

    private async Task HandleSearchWithKeyword()
    {
        _treeData = await codeService.SearchAsync(_search);
        _treeData.ReadmeContent = _homeReadmeContent;
        _tempSearch = _search;
        _currentFolderNode = _treeData;
    }

    private async Task SearchKeyUpAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            await SearchAsync();
        }
    }

    private Task SelectedFile(CodeNoteTree arg)
    {
        if (arg.CodeContainer == null)
        {
            return Task.CompletedTask;
        }
        _currentPath = arg.CurrentPaths;
        _selectedNode = arg;
        UpdateUrl(arg.CurrentPaths);
        StateHasChanged();
        return Task.CompletedTask;
    }

    private void ExpandFolder(CodeNoteTree obj)
    {
        _currentPath = obj.CurrentPaths;
        _currentFolderNode = obj;
        // 切换到文件夹视图
        _selectedNode = null;

        if (!string.IsNullOrEmpty(obj.ReadmeContent))
        {
            _treeData.ReadmeContent = obj.ReadmeContent;
        }

        UpdateUrl(obj.CurrentPaths);
        StateHasChanged();
    }

    private void UpdateUrl(IEnumerable<string> paths)
    {
        if (_isRestoringPath)
        {
            return;
        }

        var path = string.Join("/", paths);
        if (!string.Equals(path, Path, StringComparison.InvariantCulture))
        {
            var newUrl = $"/code?path={UrlEncoder.Create().Encode(path)}";
            navigationManager.NavigateTo(newUrl);
        }
    }

    private void NavigateToPath(string path)
    {
        var newUrl = string.IsNullOrEmpty(path)
            ? "/code"
            : $"/code?path={UrlEncoder.Create().Encode(path)}";
        navigationManager.NavigateTo(newUrl);
    }

    private (string first, string? keyword, string? end) MatchKeyword(string name)
    {
        if (string.IsNullOrEmpty(_search))
        {
            return (name, null, null);
        }

        var startIndex = name.IndexOf(_search, StringComparison.CurrentCultureIgnoreCase);
        if (startIndex >= 0)
        {
            var match = name.Substring(startIndex, _search.Length);
            return (name[..startIndex], match, name[(startIndex + _search.Length)..]);
        }

        return (name, null, null);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is not null)
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // Ignore
            }
        }
    }
}
