using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models.JsonViewer;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace Dpz.Core.WebMore.Pages.Tools;

public partial class JsonViewer : ComponentBase
{
    private string _jsonInput = "";
    private JsonNodeViewModel? _rootNode;
    private string _errorMessage = "";
    private string _searchText = "";
    private string _searchResult = "";
    private int _totalNodes;
    private bool _isProcessing;

    private const int FileMaxLimit = 1024 * 1024 * 20;

    private async Task ParseJsonAsync()
    {
        _errorMessage = "";
        _rootNode = null;
        _totalNodes = 0;
        _searchResult = "";
        _isProcessing = true;
        // 立即更新 UI，显示加载状态
        StateHasChanged();

        if (string.IsNullOrWhiteSpace(_jsonInput))
        {
            _isProcessing = false;
            StateHasChanged();
            return;
        }

        try
        {
            // 让出控制权，确保 UI 有机会更新显示加载动画
            await Task.Yield();

            var options = new JsonDocumentOptions
            {
                AllowTrailingCommas = true,
                CommentHandling = JsonCommentHandling.Skip,
            };

            using var document = JsonDocument.Parse(_jsonInput, options);

            // 对于大文件，分批处理以避免长时间阻塞
            await Task.Yield();
            _rootNode = BuildTree(document.RootElement, null, "root");

            await Task.Yield();
            CountNodes(_rootNode);
        }
        catch (JsonException ex)
        {
            _errorMessage = $"Invalid JSON: {ex.Message}";
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error: {ex.Message}";
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged(); // 更新 UI，隐藏加载状态
        }
    }

    private async Task LoadFile(InputFileChangeEventArgs e)
    {
        _errorMessage = "";
        _isProcessing = true;
        // 立即更新 UI，显示加载状态
        StateHasChanged();

        try
        {
            var file = e.File;
            if (file.Size > FileMaxLimit)
            {
                _errorMessage = "文件超过最大限制（20MB）";
                _isProcessing = false;
                StateHasChanged();
                return;
            }

            // 让出控制权，确保 UI 有机会更新
            await Task.Yield();

            await using var stream = file.OpenReadStream(FileMaxLimit);
            using var reader = new StreamReader(stream);
            _jsonInput = await reader.ReadToEndAsync();

            // 调用异步版本的 ParseJson
            await ParseJsonAsync();
        }
        catch (Exception ex)
        {
            _errorMessage = $"Error loading file: {ex.Message}";
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private void Clear()
    {
        _jsonInput = "";
        _rootNode = null;
        _errorMessage = "";
        _searchText = "";
        _searchResult = "";
        _totalNodes = 0;
    }

    private JsonNodeViewModel BuildTree(JsonElement element, JsonNodeViewModel? parent, string key)
    {
        var node = new JsonNodeViewModel
        {
            Key = key,
            ValueKind = element.ValueKind,
            Parent = parent,
            Depth = parent == null ? 0 : parent.Depth + 1,
            Path = parent == null ? key : $"{parent.Path}.{key}",
        };

        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    node.Children.Add(BuildTree(property.Value, node, property.Name));
                }
                break;
            case JsonValueKind.Array:
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    node.Children.Add(BuildTree(item, node, index.ToString()));
                    index++;
                }
                break;
            case JsonValueKind.String:
                node.Value = element.GetString();
                break;
            case JsonValueKind.Number:
                node.Value = element.GetRawText();
                break;
            case JsonValueKind.True:
                node.Value = true;
                break;
            case JsonValueKind.False:
                node.Value = false;
                break;
            case JsonValueKind.Null:
                node.Value = null;
                break;
        }

        return node;
    }

    private void CountNodes(JsonNodeViewModel node)
    {
        _totalNodes++;
        foreach (var child in node.Children)
        {
            CountNodes(child);
        }
    }

    private void ExpandAll()
    {
        _rootNode?.ExpandAll();
    }

    private void CollapseAll()
    {
        _rootNode?.CollapseAll();
    }

    private void OnSearchKeyUp(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            Search();
        }
    }

    private void Search()
    {
        if (_rootNode == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_searchText))
        {
            ResetSearch(_rootNode);
            _searchResult = "";
            return;
        }

        var matches = 0;
        var hasVisible = PerformSearch(_rootNode, _searchText.Trim(), ref matches);

        // If root is not visible (no matches anywhere), we should probably indicate that
        if (!hasVisible)
        {
            _searchResult = "No matches found.";
        }
        else
        {
            _searchResult = $"Found {matches} matches.";
        }
    }

    private void ResetSearch(JsonNodeViewModel node)
    {
        node.IsMatch = false;
        node.IsVisible = true;
        foreach (var child in node.Children)
        {
            ResetSearch(child);
        }
    }

    private bool PerformSearch(JsonNodeViewModel node, string term, ref int matchCount)
    {
        var selfMatch = false;

        // Check Key
        if (
            !string.IsNullOrEmpty(node.Key)
            && node.Key.Contains(term, StringComparison.OrdinalIgnoreCase)
        )
        {
            selfMatch = true;
        }

        // Check Value
        if (
            node.Value != null
            && node.Value.ToString()!.Contains(term, StringComparison.OrdinalIgnoreCase)
        )
        {
            selfMatch = true;
        }

        node.IsMatch = selfMatch;
        if (selfMatch)
        {
            matchCount++;
        }

        var childHasMatch = false;
        foreach (var child in node.Children)
        {
            var childResult = PerformSearch(child, term, ref matchCount);
            if (childResult)
            {
                childHasMatch = true;
            }
        }

        var isVisible = selfMatch || childHasMatch;
        node.IsVisible = isVisible;

        if (isVisible)
        {
            node.IsExpanded = childHasMatch;
        }

        return isVisible;
    }
}
