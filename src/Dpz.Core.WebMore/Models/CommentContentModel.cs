namespace Dpz.Core.WebMore.Models;

public class CommentContentModel
{
    /// <summary>
    /// 回复人ID
    /// </summary>
    public string? ReplyId { get; set; }
    
    /// <summary>
    /// 回复人昵称
    /// </summary>
    public string? ReplyNickName { get; set; }

    /// <summary>
    /// 回复内容
    /// </summary>
    public string? CommentText { get; set; }
}