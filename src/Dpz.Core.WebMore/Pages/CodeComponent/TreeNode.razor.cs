using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Pages.CodeComponent;

public partial class TreeNode(ICodeService codeService) : IDisposable
{
    [Parameter]
    public bool IsFolder { get; set; }

    [Parameter]
    public string Name { get; set; } = "";

    [Parameter]
    public List<string> Path { get; set; } = [];

    [Parameter]
    public string? Keyword { get; set; }

    [CascadingParameter]
    private Tree? ParentTree { get; set; }

    [CascadingParameter(Name = "ExpandedNodes")]
    public Dictionary<string, CodeNoteTree>? ExpandedNodes { get; set; }

    [CascadingParameter(Name = "ActivePath")]
    public List<string> ActivePath { get; set; } = [];

    private bool _expand;
    private CodeNoteTree? _childrenNode;
    private Tree? _childTree;
    private string _tempName = "";

    // Keep track of which active path we have responded to
    private List<string> _lastProcessedActivePath = [];

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ParentTree?.RegisterNode(this);
    }

    protected override void OnParametersSet()
    {
        base.OnParametersSet();

        // Check if we should auto-expand based on Cascading Value
        if (ExpandedNodes != null && IsFolder && !_expand && _childrenNode == null)
        {
            var pathStr = string.Join("/", Path);
            if (ExpandedNodes.TryGetValue(pathStr, out var node))
            {
                _childrenNode = node;
                _expand = true;
            }
        }
    }

    private async Task ToggleNodeAsync()
    {
        if (!IsFolder || Name == "loading...") return;

        if (!_expand)
        {
            await ExpandNodeAsync();
        }
        else
        {
            _expand = false;
        }
    }

    private async Task OnContentClickAsync()
    {
        if (Name == "loading...") return;
        
        // Notify parent (CodeView) about selection via callbacks
        // The CodeView will update the ActivePath CascadingValue
        
        if (IsFolder)
        {
            if (!_expand)
            {
                await ExpandNodeAsync();
            }
            else
            {
                if (_childrenNode != null && OnExpandFolder.HasDelegate)
                {
                    await OnExpandFolder.InvokeAsync(_childrenNode);
                }
            }
        }
        else
        {
            await SelectFileAsync();
        }
    }

    public async Task ExpandNodeAsync()
    {
        if (Name == "loading...") return;
        
        if (!IsFolder)
        {
            await SelectFileAsync();
            return;
        }

        if (!_expand || _childrenNode == null)
        {
            _tempName = Name;
            Name = "loading...";
            try 
            {
                _childrenNode = await codeService.GetTreeAsync(Path.ToArray());
                if (OnExpandFolder.HasDelegate)
                {
                    await OnExpandFolder.InvokeAsync(_childrenNode);
                }
            }
            finally
            {
                Name = _tempName;
            }
            _expand = true;
            return;
        }
        
        _expand = true;
        if (OnExpandFolder.HasDelegate && _childrenNode != null)
        {
            await OnExpandFolder.InvokeAsync(_childrenNode);
        }
    }

    [Parameter]
    public EventCallback<CodeNoteTree> OnExpandFolder { get; set; }

    [Parameter]
    public EventCallback<CodeNoteTree> OnSelectedFile { get; set; }

    private async Task SelectFileAsync()
    {
        _tempName = Name;
        Name = "loading...";
        try
        {
            var node = await codeService.GetTreeAsync(Path.ToArray());
            if (OnSelectedFile.HasDelegate)
            {
                await OnSelectedFile.InvokeAsync(node);
            }
        }
        finally
        {
            Name = _tempName;
            StateHasChanged();
        }
    }

    private (string first, string? keyword, string? end) MatchKeyword(string name)
    {
        if (string.IsNullOrEmpty(Keyword))
        {
            return (name, null, null);
        }

        var startIndex = name.IndexOf(Keyword, StringComparison.CurrentCultureIgnoreCase);
        if (startIndex >= 0)
        {
            var match = name.Substring(startIndex, Keyword.Length);
            return (name[..startIndex], match, name[(startIndex + Keyword.Length)..]);
        }

        return (name, null, null);
    }

    public void Dispose()
    {
        ParentTree?.UnregisterNode(this);
    }

    public TreeNode? FindNodeByPath(List<string> path)
    {
        return _childTree?.FindNodeByPath(path);
    }
}
