using System;
using System.Collections.Generic;

namespace Dpz.Core.WebMore.Models;

public class SteamModel
{
    public int Id { get; set; }

    /// <summary>
    /// 游戏图标
    /// </summary>
    public string? ImageIcon { get; set; }

    /// <summary>
    /// 游戏logo
    /// </summary>
    public string? ImageLogo { get; set; }

    /// <summary>
    /// 游戏名称
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// 游戏时长（单位：分钟）
    /// </summary>
    public uint PlayTime { get; set; }

    /// <summary>
    /// 成就摘要
    /// </summary>
    public List<AchievementModel> Achievements { get; set; } = [];

    /// <summary>
    /// 最后更新时间
    /// </summary>
    public DateTime LastUpdateTime { get; set; }
}
