using System;

namespace Dpz.Core.WebMore.Models;

public class TimelineModel
{
    public required string Id { get; set; }

    public required string Title { get; set; }

    public string? Content { get; set; }

    public DateTime Date { get; set; }

    public string? More { get; set; }

    /// <summary>
    /// 作者
    /// </summary>
    public required UserInfo Author { get; set; }

    public DateTime CreateTime { get; set; }

    public DateTime LastUpdateTime { get; set; }
}