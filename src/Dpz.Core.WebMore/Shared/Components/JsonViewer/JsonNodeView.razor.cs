using Dpz.Core.WebMore.Models.JsonViewer;
using Microsoft.AspNetCore.Components;
using System.Text.Json;

namespace Dpz.Core.WebMore.Shared.Components.JsonViewer;

public partial class JsonNodeView
{
    [Parameter]
    public JsonNodeViewModel? Node { get; set; }

    private void ToggleExpand()
    {
        if (Node != null)
        {
            Node.IsExpanded = !Node.IsExpanded;
        }
    }

    private string GetDisplayValue(JsonNodeViewModel node)
    {
        if (node.ValueKind == JsonValueKind.Null) return "null";
        if (node.ValueKind == JsonValueKind.String) return $"\"{node.Value}\"";
        return node.Value?.ToString() ?? "null";
    }
}

