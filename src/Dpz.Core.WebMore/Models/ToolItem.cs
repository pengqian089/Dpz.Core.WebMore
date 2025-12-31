namespace Dpz.Core.WebMore.Models;

/// <summary>
/// 工具项模型
/// </summary>
public class ToolItem
{
    /// <summary>
    /// 工具名称
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// 工具描述
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 图标类名 (Font Awesome)
    /// </summary>
    public string Icon { get; set; } = "fa fa-tools";

    /// <summary>
    /// 工具URL路径
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// 工具分类
    /// </summary>
    public string Category { get; set; } = "其他";

    /// <summary>
    /// 是否精确匹配路由
    /// </summary>
    public bool ExactMatch { get; set; }

    /// <summary>
    /// 是否为新工具
    /// </summary>
    public bool IsNew { get; set; }

    /// <summary>
    /// 是否禁用
    /// </summary>
    public bool IsDisabled { get; set; } = false;
}