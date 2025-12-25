using System.Collections.Generic;
using System.Linq;
using Dpz.Core.WebMore.Models;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Pages.CodeComponent;

public partial class Tree
{
    [Parameter]
    public CodeNoteTree Node { get; set; } = new();

    [Parameter]
    public EventCallback<CodeNoteTree> OnSelectedFile { get; set; }

    [Parameter]
    public EventCallback<CodeNoteTree> OnExpandFolder { get; set; }

    [Parameter]
    public string? Keyword { get; set; }

    private readonly List<TreeNode> _nodes = [];

    protected override void OnInitialized()
    {
        _nodes.Clear();
        base.OnInitialized();
    }

    public void RegisterNode(TreeNode? node)
    {
        if (node is not null && !_nodes.Contains(node))
        {
            _nodes.Add(node);
        }
    }

    public void UnregisterNode(TreeNode node)
    {
        _nodes.Remove(node);
    }

    public TreeNode? FindNodeByPath(List<string> path)
    {
        var directChild = _nodes.FirstOrDefault(n => n.Path.SequenceEqual(path));
        if (directChild != null)
        {
            return directChild;
        }

        // It's a descendant. Find the ancestor node in the current tree level.
        var ancestor = _nodes.FirstOrDefault(n =>
            n.IsFolder && path.Count > n.Path.Count && path.Take(n.Path.Count).SequenceEqual(n.Path)
        );

        return ancestor?.FindNodeByPath(path);
    }
}
