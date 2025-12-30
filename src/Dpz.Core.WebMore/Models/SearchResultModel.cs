namespace Dpz.Core.WebMore.Models;

public class SearchResultModel
{
    /// <summary>
    /// 关键字所在行
    /// </summary>
    public int LineNumber { get; set; }
    
    /// <summary>
    /// 关键字所在行的起始索引
    /// </summary>
    public int StartIndex { get; set; }
    
    /// <summary>
    /// 关键字所在行的结束索引
    /// </summary>
    public int EndIndex { get; set; }
    
    /// <summary>
    /// 匹配文本
    /// </summary>
    public string? MatchedText { get; set; }
}