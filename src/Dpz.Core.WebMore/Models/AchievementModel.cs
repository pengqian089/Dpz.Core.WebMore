using System;

namespace Dpz.Core.WebMore.Models;

public class AchievementModel
{
    /// <summary>
    /// 名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 默认值
    /// </summary>
    public int DefaultValue { get; set; }

    /// <summary>
    /// 显示名称
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// 是否为隐藏成就
    /// </summary>
    public bool Hidden { get; set; }

    /// <summary>
    /// 描述
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// 已解锁成就图标
    /// </summary>
    public string? Icon { get; set; }

    /// <summary>
    /// 未解锁成就图标
    /// </summary>
    public string? IconGray { get; set; }

    /// <summary>
    /// 成就解锁时间
    /// </summary>
    public DateTime? UnlockTime { get; set; }
}