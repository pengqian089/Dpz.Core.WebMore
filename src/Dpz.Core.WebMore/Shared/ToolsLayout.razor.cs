using System;
using System.Collections.Generic;
using System.Linq;
using Dpz.Core.WebMore.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;

namespace Dpz.Core.WebMore.Shared;

public partial class ToolsLayout(NavigationManager navigationManager) : IDisposable
{
    private string _searchKeyword = string.Empty;
    private string _selectedCategory = "全部";

    private List<ToolItem> _allTools = [];
    private List<ToolItem> _filteredTools = [];
    private List<string> _categories = [];

    protected override void OnInitialized()
    {
        InitializeTools();
        FilterTools();
        navigationManager.LocationChanged += OnLocationChanged;
    }

    private void InitializeTools()
    {
        _allTools =
        [
            new ToolItem
            {
                Name = "对话框测试",
                Description = "测试对话框、通知和 Toast 组件",
                Icon = "fa fa-vial",
                Url = "tools",
                Category = "测试工具",
                ExactMatch = true,
                IsNew = false,
            },
            new ToolItem
            {
                Name = "BSON 转 JSON",
                Description = "将 BSON 文件转换为 JSON 格式",
                Icon = "fa fa-file-code",
                Url = "tools/bson-to-json",
                Category = "数据转换",
                ExactMatch = false,
                IsNew = false,
            },
            new ToolItem
            {
                Name = "JSON 查看器",
                Description = "格式化和查看 JSON 数据",
                Icon = "fa fa-code",
                Url = "tools/json-viewer",
                Category = "数据转换",
                ExactMatch = false,
                IsNew = false,
            },
            new ToolItem
            {
                Name = "正则表达式测试",
                Description = "测试和调试正则表达式，支持匹配高亮",
                Icon = "fa fa-asterisk",
                Url = "tools/regex-test",
                Category = "文本处理",
                ExactMatch = false,
                IsNew = true,
            },
        ];

        // 提取所有分类
        _categories = _allTools.Select(t => t.Category).Distinct().OrderBy(c => c).ToList();
    }

    private void FilterTools()
    {
        var query = _allTools.AsEnumerable();

        // 分类筛选
        if (_selectedCategory != "全部")
        {
            query = query.Where(t => t.Category == _selectedCategory);
        }

        // 搜索关键词筛选
        if (!string.IsNullOrWhiteSpace(_searchKeyword))
        {
            var keyword = _searchKeyword.Trim().ToLower();
            query = query.Where(t =>
                t.Name.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)
                || t.Description.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)
                || t.Category.Contains(keyword, StringComparison.CurrentCultureIgnoreCase)
            );
        }

        _filteredTools = query.ToList();
    }

    private void OnSearchChanged()
    {
        FilterTools();
        StateHasChanged();
    }

    private void ClearSearch()
    {
        _searchKeyword = string.Empty;
        FilterTools();
        StateHasChanged();
    }

    private void SelectCategory(string category)
    {
        _selectedCategory = category;
        FilterTools();
        StateHasChanged();
    }

    private string GetCategoryIcon(string category)
    {
        return category switch
        {
            "数据转换" => "fa fa-exchange-alt",
            "测试工具" => "fa fa-flask",
            "文本处理" => "fa fa-file-text",
            "编码解码" => "fa fa-key",
            "图片处理" => "fa fa-image",
            "网络工具" => "fa fa-network-wired",
            "开发工具" => "fa fa-wrench",
            _ => "fa fa-tools",
        };
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        StateHasChanged();
    }

    public void Dispose()
    {
        navigationManager.LocationChanged -= OnLocationChanged;
    }
}
