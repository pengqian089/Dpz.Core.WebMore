using System.Text.Json;
using System.Collections.Generic;

namespace Dpz.Core.WebMore.Models.JsonViewer;

public class JsonNodeViewModel
{
    public string? Key { get; set; }
    public object? Value { get; set; }
    public JsonValueKind ValueKind { get; set; }
    public List<JsonNodeViewModel> Children { get; set; } = [];
    public bool IsExpanded { get; set; } = true;
    public bool IsVisible { get; set; } = true;
    public bool IsMatch { get; set; }
    public JsonNodeViewModel? Parent { get; set; }
    public int Depth { get; set; }

    /// <summary>
    /// Path to this node (for reference)
    /// </summary>
    public string Path { get; set; } = "";

    public void ExpandAll()
    {
        IsExpanded = true;
        foreach (var child in Children)
        {
            child.ExpandAll();
        }
    }

    public void CollapseAll()
    {
        IsExpanded = false;
        foreach (var child in Children)
        {
            child.CollapseAll();
        }
    }
}

