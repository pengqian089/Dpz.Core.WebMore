using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Pages.CodeComponent;

public partial class TreeNode:IDisposable
{
    [Parameter] public bool IsFolder { get; set; }

    [Parameter] public string Name { get; set; } = "";

    [Parameter] public List<string> Path { get; set; }

    [Parameter] public string? Keyword { get; set; }

    [Inject] private ICodeService CodeService { get; set; }

    private bool _expand = false;

    private CodeNoteTree? _childrenNode;

    private const string Active = "background-color: rgb(196 224 255 / 12%)";
    private static List<string>? _activePath = null;

    private async Task ExpandNodeAsync()
    {
        if (Name == "loading...")
            return;
        _activePath = Path;
        if (!IsFolder)
        {
            await SelectFileAsync();
            return;
        }

        if (!_expand)
        {
            _tempName = Name;
            Name = "loading...";
            _childrenNode ??= await CodeService.GetTreeAsync(Path.ToArray());
            await OnExpandFolder.InvokeAsync(_childrenNode);
            Name = _tempName;
        }
        // else 
        //     _childrenNode = null;

        _expand = !_expand;
    }

    [Parameter] public EventCallback<CodeNoteTree> OnExpandFolder { get; set; }

    [Parameter] public EventCallback<CodeNoteTree> OnSelectedFile { get; set; }

    private string _tempName = "";

    private async Task SelectFileAsync()
    {
        _tempName = Name;
        Name = "loading...";
        var node = await CodeService.GetTreeAsync(Path.ToArray());
        await OnSelectedFile.InvokeAsync(node);
        Name = _tempName;
        StateHasChanged();
    }

    (string first, string? keyword, string? end) MatchKeyword(string name)
    {
        if (string.IsNullOrEmpty(Keyword))
            return (name, null, null);

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
        if (_activePath != null)
        {
            Console.WriteLine("dispose");
            _activePath = null;    
        }
        
    }
}