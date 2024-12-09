using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages;

public partial class Code
{
    [Inject] private ICodeService CodeService { get; set; }
    
    [Inject] private IJSRuntime JsRuntime { get; set; }

    private CodeNoteTree? _treeData = null;

    private bool _isLoading;

    private IEnumerable<string> _currentPath = Array.Empty<string>();
    
    protected override async Task OnInitializedAsync()
    {
        await LoadTreeData(null);
        await base.OnInitializedAsync();
    }
    
    private async Task LoadTreeData(IEnumerable<string>? path)
    {
        _isLoading = true;
        path ??= Array.Empty<string>();
        var currentPath = path.ToArray();
        _treeData = await CodeService.GetTreeAsync(currentPath);
        _currentPath = currentPath;
        _isLoading = false;
    }
}