using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Threading.Tasks;
using Dpz.Core.EnumLibrary;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Pages.CodeComponent;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;
using MudBlazor;

namespace Dpz.Core.WebMore.Pages;

public partial class CodeView(
    ICodeService codeService,
    NavigationManager navigationManager,
    IAppDialogService dialogService,
    ILogger<CodeView> logger
)
{
    [SupplyParameterFromQuery]
    [Parameter]
    public string? Path { get; set; }

    private CodeNoteTree _treeData = new();

    private bool _isLoading;

    private IEnumerable<string> _currentPath = [];

    protected override async Task OnInitializedAsync()
    {
        logger.LogInformation("组件初始化开始");
        await LoadTreeData(null);
        logger.LogInformation(
            "组件初始化完成，树数据: {TreeData}",
            JsonSerializer.Serialize(_treeData)
        );
        await base.OnInitializedAsync();
    }

    protected override async Task OnParametersSetAsync()
    {
        logger.LogInformation(
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
            logger.LogInformation("路径发生变化，开始处理");
            var paths = Path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var index = _tabs.FindIndex(x => x.CurrentPaths.SequenceEqual(paths));
            if (index >= 0)
            {
                ActiveTabIndex = index + 1;
                return;
            }
            var tempPaths = new List<string>();

            // 等待根目录加载完成
            if (_treeData.Type == FileSystemType.NoExists || _isLoading)
            {
                logger.LogInformation(
                    "等待根目录加载，当前状态: TreeData={HasTreeData}, IsLoading={IsLoading}",
                    _treeData.Type,
                    _isLoading
                );
                await Task.Delay(100);
                await OnParametersSetAsync();
                return;
            }

            foreach (var item in paths)
            {
                tempPaths.Add(item);
                logger.LogInformation("处理路径: {Path}", string.Join("/", tempPaths));
                var tree = await codeService.GetTreeAsync(tempPaths.ToArray());
                logger.LogInformation("获取到树数据: {TreeData}", JsonSerializer.Serialize(tree));

                // 找到当前层级的节点
                var node = _treeComponent.FindNodeByPath(tempPaths);
                logger.LogInformation("查找节点结果: {Result}", node != null ? "找到" : "未找到");

                if (node == null)
                {
                    // 如果找不到节点，说明父节点没有展开
                    // 找到父节点并展开
                    var parentPaths = tempPaths.Take(tempPaths.Count - 1).ToList();
                    logger.LogInformation(
                        "尝试查找父节点: {ParentPath}",
                        string.Join("/", parentPaths)
                    );
                    var parentNode = _treeComponent.FindNodeByPath(parentPaths);
                    if (parentNode != null)
                    {
                        logger.LogInformation("找到父节点，开始展开");
                        await parentNode.ExpandNodeAsync();
                        // 重新查找当前节点
                        node = _treeComponent.FindNodeByPath(tempPaths);
                        logger.LogInformation(
                            "重新查找节点结果: {Result}",
                            node != null ? "找到" : "未找到"
                        );
                    }
                }

                if (node != null)
                {
                    if (tree.IsDirectory)
                    {
                        logger.LogInformation("展开目录节点");
                        await node.ExpandNodeAsync();
                    }
                    else
                    {
                        logger.LogInformation("选择文件节点");
                        await node.SelectFileAsync();
                    }
                }
                StateHasChanged();
            }
        }
        await base.OnParametersSetAsync();
        logger.LogInformation("参数设置完成");
    }

    private string? _homeReadmeContent;

    private async Task LoadTreeData(IEnumerable<string>? path)
    {
        logger.LogInformation("开始加载树数据，路径: {Path}", string.Join("/", path ?? []));
        _isLoading = true;
        path ??= [];
        var currentPath = path.ToArray();
        _treeData = await codeService.GetTreeAsync(currentPath);
        logger.LogInformation("树数据加载完成: {TreeData}", JsonSerializer.Serialize(_treeData));
        _homeReadmeContent = _treeData.ReadmeContent;
        _currentPath = currentPath;
        _isLoading = false;
    }

    private string? _search;
    private string? _tempSearch;

    private async Task SearchAsync()
    {
        logger.LogInformation(
            "开始搜索，关键字: {Search}, 临时关键字: {TempSearch}",
            _search,
            _tempSearch
        );
        if (string.IsNullOrEmpty(_search) && string.IsNullOrEmpty(_tempSearch))
        {
            logger.LogWarning("搜索关键字为空");
            dialogService.Toast("请输入关键字！", ToastType.Warning);
            return;
        }

        if (_search == _tempSearch)
        {
            logger.LogInformation("搜索关键字未变化");
            return;
        }

        _isLoading = true;
        try
        {
            if (string.IsNullOrEmpty(_search))
            {
                logger.LogInformation("执行空搜索");
                await HandleEmptySearch();
            }
            else
            {
                logger.LogInformation("执行关键字搜索");
                await HandleSearchWithKeyword();
            }
        }
        finally
        {
            _isLoading = false;
            logger.LogInformation("搜索完成");
        }
    }

    private async Task HandleEmptySearch()
    {
        _tempSearch = null;
        _treeData = await codeService.GetTreeAsync(null);
        logger.LogInformation(
            "空搜索完成，树数据: {TreeData}",
            JsonSerializer.Serialize(_treeData)
        );
        _dynamicTabs.ActivatePanel(0);
        navigationManager.NavigateTo("/code");
    }

    private async Task HandleSearchWithKeyword()
    {
        _treeData = await codeService.SearchAsync(_search);
        logger.LogInformation(
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
            logger.LogInformation("触发回车搜索");
            await SearchAsync();
        }
    }

    private Task SelectedFile(CodeNoteTree arg)
    {
        logger.LogInformation("选择文件: {FileData}", JsonSerializer.Serialize(arg));
        if (arg?.CodeContainer == null)
        {
            logger.LogWarning("文件内容为空");
            return Task.CompletedTask;
        }
        _currentPath = arg.CurrentPaths;
        var tabIndex = _tabs.FindIndex(x => x.CurrentPaths.SequenceEqual(arg.CurrentPaths));
        if (tabIndex == -1)
        {
            logger.LogInformation("添加新标签页");
            AddTab(arg);
        }
        else
        {
            logger.LogInformation("激活已有标签页");
            ActiveTabIndex = tabIndex + 1;
        }
        return Task.CompletedTask;
    }

    private void ExpandFolder(CodeNoteTree obj)
    {
        logger.LogInformation("展开文件夹: {FolderData}", JsonSerializer.Serialize(obj));
        _currentPath = obj.CurrentPaths;
        _readmePath = obj.CurrentPaths;
        if (!string.IsNullOrEmpty(obj.ReadmeContent))
        {
            logger.LogInformation("更新 README 内容");
            _treeData.ReadmeContent = obj.ReadmeContent;
            ActiveTabIndex = 0;
        }
        else
        {
            UpdateUrl(obj.CurrentPaths);
        }
    }

    private void UpdateUrl(IEnumerable<string> paths)
    {
        var path = string.Join("/", paths);
        logger.LogInformation("更新 URL，当前路径: {CurrentPath}, 新路径: {NewPath}", path, Path);
        if (!string.Equals(path, Path, StringComparison.InvariantCulture))
        {
            var newUrl = $"/code?path={UrlEncoder.Create().Encode(path)}";
            logger.LogInformation("导航到新 URL: {NewUrl}", newUrl);
            navigationManager.NavigateTo(newUrl);
        }
    }

    #region tabs

    private MudDynamicTabs _dynamicTabs;
    private readonly List<CodeNoteTree> _tabs = [];
    private bool _stateHasChanged;
    private Tree _treeComponent;
    private IEnumerable<string> _readmePath = [];

    private int _activeTabIndex;
    private int ActiveTabIndex
    {
        get => _activeTabIndex;
        set
        {
            if (_activeTabIndex == value)
            {
                return;
            }
            logger.LogInformation(
                "标签页切换，旧索引: {OldIndex}, 新索引: {NewIndex}",
                _activeTabIndex,
                value
            );
            _activeTabIndex = value;
            switch (_activeTabIndex)
            {
                case 0:
                    UpdateUrl(_readmePath);
                    break;
                case > 0 when _activeTabIndex <= _tabs.Count:
                {
                    var tab = _tabs[_activeTabIndex - 1];
                    UpdateUrl(tab.CurrentPaths);
                    break;
                }
            }
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (_stateHasChanged)
        {
            logger.LogInformation("触发状态更新");
            _stateHasChanged = false;
            StateHasChanged();
        }
    }

    private void AddTab(CodeNoteTree node)
    {
        logger.LogInformation("添加标签页: {TabData}", JsonSerializer.Serialize(node));
        _tabs.Add(node);
        ActiveTabIndex = _tabs.Count;
        _stateHasChanged = true;
    }

    private void RemoveTab(object id)
    {
        logger.LogInformation("移除标签页: {TabId}", id);
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
