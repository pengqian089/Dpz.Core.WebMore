using System;

namespace Dpz.Core.WebMore.Models;

public class MumbleModel
{
    public required string Id { get; set; }

    /// <summary>
    /// Markdown
    /// </summary>
    public required string Markdown { get; set; }

    /// <summary>
    /// 发表时间
    /// </summary>
    public DateTime CreateTime { get; set; }

    /// <summary>
    /// 最后修改时间
    /// </summary>
    public DateTime LastUpdateTime { get; set; }

    public int Like { get; set; }

    /// <summary>
    /// 回复
    /// </summary>
    public int CommentCount { get; set; }

    public required UserInfo Author { get; set; }
        
    public bool ShowComment { get; set; }
}