using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Pages.CodeComponent;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;

namespace Dpz.Core.WebMore.Pages;

public partial class CodeView
{
    [SupplyParameterFromQuery]
    [Parameter]
    public string Path { get; set; }

    [Inject]
    private ICodeService CodeService { get; set; }

    [Inject]
    private IJSRuntime JsRuntime { get; set; }

    [Inject]
    private NavigationManager NavigationManager { get; set; }

    [Inject]
    private ILogger<CodeView> Logger { get; set; }

    private CodeNoteTree _treeData = new();

    private bool _isLoading;

    private IEnumerable<string> _currentPath = [];

    protected override async Task OnInitializedAsync()
    {
        Logger.LogInformation("组件初始化开始");
        await LoadTreeData(null);
        Logger.LogInformation(
            "组件初始化完成，树数据: {TreeData}",
            JsonSerializer.Serialize(_treeData)
        );
        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        Logger.LogInformation(
            "参数设置开始，当前路径: {CurrentPath}, 新路径: {NewPath}",
            string.Join("/", _currentPath),
            Path
        );
        var path = string.Join("/", _currentPath);
        if (
            !string.IsNullOrEmpty(Path)
            && !string.Equals(path, Path, StringComparison.InvariantCulture)
        )
        {
            Logger.LogInformation("路径发生变化，开始处理");
            var paths = Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var tempPaths = new List<string>();

            // 等待根目录加载完成
            if (_treeData == null || _isLoading)
            {
                Logger.LogInformation(
                    "等待根目录加载，当前状态: TreeData={HasTreeData}, IsLoading={IsLoading}",
                    _treeData != null,
                    _isLoading
                );
                await Task.Delay(100);
                await OnParametersSetAsync();
                return;
            }

            foreach (var item in paths)
            {
                tempPaths.Add(item);
                Logger.LogInformation("处理路径: {Path}", string.Join("/", tempPaths));
                var tree = await CodeService.GetTreeAsync(tempPaths.ToArray());
                Logger.LogInformation("获取到树数据: {TreeData}", JsonSerializer.Serialize(tree));

                // 找到当前层级的节点
                var node = _treeComponent.FindNodeByPath(tempPaths);
                Logger.LogInformation("查找节点结果: {Result}", node != null ? "找到" : "未找到");

                if (node == null)
                {
                    // 如果找不到节点，说明父节点没有展开
                    // 找到父节点并展开
                    var parentPaths = tempPaths.Take(tempPaths.Count - 1).ToList();
                    Logger.LogInformation(
                        "尝试查找父节点: {ParentPath}",
                        string.Join("/", parentPaths)
                    );
                    var parentNode = _treeComponent.FindNodeByPath(parentPaths);
                    if (parentNode != null)
                    {
                        Logger.LogInformation("找到父节点，开始展开");
                        await parentNode.ExpandNodeAsync();
                        // 重新查找当前节点
                        node = _treeComponent.FindNodeByPath(tempPaths);
                        Logger.LogInformation(
                            "重新查找节点结果: {Result}",
                            node != null ? "找到" : "未找到"
                        );
                    }
                }

                if (node != null)
                {
                    if (tree.IsDirectory)
                    {
                        Logger.LogInformation("展开目录节点");
                        await node.ExpandNodeAsync();
                    }
                    else
                    {
                        Logger.LogInformation("选择文件节点");
                        await node.SelectFileAsync();
                    }
                }
                StateHasChanged();
            }
        }
        await base.OnParametersSetAsync();
        Logger.LogInformation("参数设置完成");
    }

    private string _homeReadmeContent;

    private async Task LoadTreeData(IEnumerable<string> path)
    {
        Logger.LogInformation(
            "开始加载树数据，路径: {Path}",
            string.Join("/", path ?? Array.Empty<string>())
        );
        _isLoading = true;
        path ??= [];
        var currentPath = path.ToArray();
        _treeData = await CodeService.GetTreeAsync(currentPath);
        Logger.LogInformation("树数据加载完成: {TreeData}", JsonSerializer.Serialize(_treeData));
        _homeReadmeContent = _treeData.ReadmeContent;
        _currentPath = currentPath;
        _isLoading = false;
    }

    private string _search;
    private string _tempSearch;

    [Inject]
    private ISnackbar Snackbar { get; set; }

    private async Task SearchAsync()
    {
        Logger.LogInformation(
            "开始搜索，关键字: {Search}, 临时关键字: {TempSearch}",
            _search,
            _tempSearch
        );
        if (string.IsNullOrEmpty(_search) && string.IsNullOrEmpty(_tempSearch))
        {
            Logger.LogWarning("搜索关键字为空");
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.TopCenter;
            Snackbar.Add("请输入关键字！", Severity.Warning);
            return;
        }

        if (_search == _tempSearch)
        {
            Logger.LogInformation("搜索关键字未变化");
            return;
        }

        _isLoading = true;
        try
        {
            if (string.IsNullOrEmpty(_search))
            {
                Logger.LogInformation("执行空搜索");
                await HandleEmptySearch();
            }
            else
            {
                Logger.LogInformation("执行关键字搜索");
                await HandleSearchWithKeyword();
            }
        }
        finally
        {
            _isLoading = false;
            Logger.LogInformation("搜索完成");
        }
    }

    private async Task HandleEmptySearch()
    {
        _tempSearch = null;
        _treeData = await CodeService.GetTreeAsync(null);
        Logger.LogInformation(
            "空搜索完成，树数据: {TreeData}",
            JsonSerializer.Serialize(_treeData)
        );
        _dynamicTabs.ActivatePanel(0);
        NavigationManager.NavigateTo("/code");
    }

    private async Task HandleSearchWithKeyword()
    {
        _treeData = await CodeService.SearchAsync(_search);
        Logger.LogInformation(
            "关键字搜索完成，树数据: {TreeData}",
            JsonSerializer.Serialize(_treeData)
        );
        _treeData.ReadmeContent = _homeReadmeContent;
        _tempSearch = _search;
    }

    private async Task SearchKeyUpAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            Logger.LogInformation("触发回车搜索");
            await SearchAsync();
        }
    }

    private Task SelectedFile(CodeNoteTree arg)
    {
        Logger.LogInformation("选择文件: {FileData}", JsonSerializer.Serialize(arg));
        if (arg?.CodeContainer == null)
        {
            Logger.LogWarning("文件内容为空");
            return Task.CompletedTask;
        }
        var tab = _tabs.SingleOrDefault(x => x.CurrentPaths.SequenceEqual(arg.CurrentPaths));
        if (tab is null)
        {
            Logger.LogInformation("添加新标签页");
            AddTab(arg);
        }
        else
        {
            Logger.LogInformation("激活已有标签页");
            _dynamicTabs.ActivatePanel(_tabs.IndexOf(tab) + 1);
        }
        _currentPath = arg.CurrentPaths;

        UpdateUrl(arg.CurrentPaths);
        return Task.CompletedTask;
    }

    private void ExpandFolder(CodeNoteTree obj)
    {
        Logger.LogInformation("展开文件夹: {FolderData}", JsonSerializer.Serialize(obj));
        if (!string.IsNullOrEmpty(obj.ReadmeContent))
        {
            Logger.LogInformation("更新 README 内容");
            _treeData.ReadmeContent = obj.ReadmeContent;
            _dynamicTabs.ActivatePanel(0);
        }
        _currentPath = obj.CurrentPaths;

        UpdateUrl(obj.CurrentPaths);
    }

    private void UpdateUrl(IEnumerable<string> paths)
    {
        var path = string.Join("/", paths);
        Logger.LogInformation("更新 URL，当前路径: {CurrentPath}, 新路径: {NewPath}", path, Path);
        if (!string.Equals(path, Path, StringComparison.InvariantCulture))
        {
            var newUrl = $"/code?path={UrlEncoder.Create().Encode(path)}";
            Logger.LogInformation("导航到新 URL: {NewUrl}", newUrl);
            NavigationManager.NavigateTo(newUrl);
        }
    }

    #region tabs

    private MudDynamicTabs _dynamicTabs;
    private readonly List<CodeNoteTree> _tabs = new();
    private int _tabIndex;
    private bool _stateHasChanged;
    private Tree _treeComponent;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (_stateHasChanged)
        {
            Logger.LogInformation("触发状态更新");
            _stateHasChanged = false;
            StateHasChanged();
        }
    }

    private void AddTab(CodeNoteTree node)
    {
        Logger.LogInformation("添加标签页: {TabData}", JsonSerializer.Serialize(node));
        _tabs.Add(node);
        _tabIndex = _tabs.Count;
        _stateHasChanged = true;
    }

    private void RemoveTab(object id)
    {
        Logger.LogInformation("移除标签页: {TabId}", id);
        var tabView = _tabs.SingleOrDefault(
            (x) => id is string ids && string.Join("-", x.CurrentPaths) == ids
        );
        if (tabView is not null)
        {
            _tabs.Remove(tabView);
            _stateHasChanged = true;
        }
    }

    private void CloseTabCallback(MudTabPanel panel) => RemoveTab(panel.ID);

    #endregion
}
