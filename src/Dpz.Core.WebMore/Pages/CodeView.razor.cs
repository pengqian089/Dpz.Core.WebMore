using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using MudBlazor;

namespace Dpz.Core.WebMore.Pages;

public partial class CodeView
{
    [Inject] private ICodeService CodeService { get; set; }

    [Inject] private IJSRuntime JsRuntime { get; set; }

    private CodeNoteTree _treeData = new();

    private bool _isLoading;

    private IEnumerable<string> _currentPath = Array.Empty<string>();

    protected override async Task OnInitializedAsync()
    {
        await LoadTreeData(null);
        await base.OnInitializedAsync();
    }

    private string _homeReadmeContent;

    private async Task LoadTreeData(IEnumerable<string> path)
    {
        _isLoading = true;
        path ??= Array.Empty<string>();
        var currentPath = path.ToArray();
        _treeData = await CodeService.GetTreeAsync(currentPath);
        _homeReadmeContent = _treeData.ReadmeContent;
        _currentPath = currentPath;
        _isLoading = false;
    }

    private string _search;
    private string _tempSearch;
    [Inject] private ISnackbar Snackbar { get; set; }

    private async Task SearchAsync()
    {
        if (string.IsNullOrEmpty(_search) && string.IsNullOrEmpty(_tempSearch))
        {
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.TopCenter;
            Snackbar.Add("请输入关键字！", Severity.Warning);
            return;
        }

        if (_search == _tempSearch)
            return;
        _isLoading = true;
        if (string.IsNullOrEmpty(_search))
        {
            _tempSearch = null;
            _treeData = await CodeService.GetTreeAsync(null);
            _dynamicTabs.ActivatePanel(0);
        }
        else
        {
            _treeData = await CodeService.SearchAsync(_search);
            _treeData.ReadmeContent = _homeReadmeContent;
            _tempSearch = _search;
        }

        _isLoading = false;
    }

    private async Task SearchKeyUpAsync(KeyboardEventArgs args)
    {
        if (args.Key == "Enter")
        {
            await SearchAsync();
        }
    }

    //private CodeNoteTree? _selectedNode;
    private Task SelectedFile(CodeNoteTree arg)
    {
        if (arg?.CodeContainer == null)
            return Task.CompletedTask;
        var tab = _tabs.SingleOrDefault(x => x.CurrentPaths.SequenceEqual(arg.CurrentPaths));
        if (tab is null)
        {
            AddTab(arg);
        }
        else
        {
            _dynamicTabs.ActivatePanel(_tabs.IndexOf(tab) + 1);
        }

        return Task.CompletedTask;
    }

    private void ExpandFolder(CodeNoteTree obj)
    {
        if (!string.IsNullOrEmpty(obj.ReadmeContent))
        {
            _treeData.ReadmeContent = obj.ReadmeContent;
            _dynamicTabs.ActivatePanel(0);
            //StateHasChanged();
        }
    }

    #region tabs

    private MudDynamicTabs _dynamicTabs;
    private readonly List<CodeNoteTree> _tabs = new();
    private int _tabIndex;
    bool _stateHasChanged;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        await base.OnAfterRenderAsync(firstRender);
        if (_stateHasChanged)
        {
            _stateHasChanged = false;
            StateHasChanged();
        }
    }

    private void AddTab(CodeNoteTree node)
    {
        _tabs.Add(node);
        _tabIndex = _tabs.Count;
        _stateHasChanged = true;
    }

    private void RemoveTab(object id)
    {
        var tabView = _tabs.SingleOrDefault((x) => id is string ids && string.Join("-", x.CurrentPaths) == ids);
        if (tabView is not null)
        {
            _tabs.Remove(tabView);
            _stateHasChanged = true;
        }
    }

    private void CloseTabCallback(MudTabPanel panel) => RemoveTab(panel.ID);

    #endregion
}