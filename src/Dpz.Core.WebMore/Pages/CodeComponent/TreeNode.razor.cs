using System;
using System.Collections.Generic;
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

    private bool _expand;

    private CodeNoteTree? _childrenNode;

    private Tree? _childTree;

    private static List<string> _activePath = [];

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ParentTree?.RegisterNode(this);
    }

    private async Task ToggleNodeAsync()
    {
        if (!IsFolder || Name == "loading...")
        {
            return;
        }

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
        if (Name == "loading...")
        {
            return;
        }

        // Always update active path on click
        _activePath = Path;

        if (IsFolder)
        {
            if (!_expand)
            {
                await ExpandNodeAsync();
            }
            else
            {
                // Already expanded, just notify parent to update view (e.g. breadcrumbs, folder content)
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
        if (Name == "loading...")
        {
            return;
        }

        _activePath = Path;

        if (!IsFolder)
        {
            await SelectFileAsync();
            return;
        }

        // Only load if not already expanded or children not loaded
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

        // If already expanded and loaded, just ensure it stays expanded
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

    private string _tempName = "";

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
