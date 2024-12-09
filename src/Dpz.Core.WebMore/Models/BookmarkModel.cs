using System.Collections.Generic;

namespace Dpz.Core.WebMore.Models;

public class BookmarkModel
{
    public required string Id { get; set; }
    
    /// <summary>
    /// 图标地址
    /// </summary>
    public string Icon { get; set; }
    
    /// <summary>
    /// 图片地址
    /// </summary>
    public string Image { get; set; }
    
    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; }
    
    /// <summary>
    /// URL地址
    /// </summary>
    public string Url { get; set; }

    /// <summary>
    /// 分类
    /// </summary>
    public List<string> Categories { get; set; } = [];
}