using System.Collections.Generic;

namespace Dpz.Core.WebMore.Models;

/// <summary>
/// “忆往昔”碎碎念展示模型
/// </summary>
public class MumbleHistoryViewModel
{
    /// <summary>
    /// 原始数据
    /// </summary>
    public required MumbleModel Model { get; set; }

    /// <summary>
    /// 已移除图片的正文
    /// </summary>
    public string Html { get; set; } = "";

    /// <summary>
    /// 图片列表
    /// </summary>
    public List<MumbleImageModel> Images { get; set; } = [];
}
