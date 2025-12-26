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

    private const string Active = "background-color: rgb(196 224 255 / 12%)";
    private static List<string> _activePath = [];

    protected override void OnInitialized()
    {
        base.OnInitialized();
        ParentTree?.RegisterNode(this);
    }

    public async Task<bool> ExpandNodeAsync()
    {
        if (Name == "loading...")
        {
            return false;
        }
        _activePath = Path;
        if (!IsFolder)
        {
            await SelectFileAsync();
            return true;
        }

        if (!_expand)
        {
            _tempName = Name;
            Name = "loading...";
            _childrenNode = await codeService.GetTreeAsync(Path.ToArray());
            await OnExpandFolder.InvokeAsync(_childrenNode);
            Name = _tempName;
            _expand = true;
            return true;
        }
        else
        {
            _expand = false;
            return true;
        }
    }

    [Parameter]
    public EventCallback<CodeNoteTree> OnExpandFolder { get; set; }

    [Parameter]
    public EventCallback<CodeNoteTree> OnSelectedFile { get; set; }

    private string _tempName = "";

    public async Task SelectFileAsync()
    {
        _tempName = Name;
        Name = "loading...";
        var node = await codeService.GetTreeAsync(Path.ToArray());
        await OnSelectedFile.InvokeAsync(node);
        Name = _tempName;
        StateHasChanged();
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

    // private bool _select = false;
    // private async Task SelectNodeAsync()
    // {
    //     if (!IsFolder)
    //         return;
    //     if(!_expand)
    //         _childrenNode = await CodeService.GetTreeAsync(Path.ToArray());
    //     else
    //         _childrenNode = null;
    //
    //     _expand = !_expand;
    // }
    public void Dispose()
    {
        if (_activePath.Count == 0)
        {
            Console.WriteLine("dispose");
            _activePath = [];
        }
        ParentTree?.UnregisterNode(this);
    }

    public TreeNode? FindNodeByPath(List<string> path)
    {
        return _childTree?.FindNodeByPath(path);
    }
}
