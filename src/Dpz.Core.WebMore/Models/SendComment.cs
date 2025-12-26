using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Dpz.Core.EnumLibrary;

namespace Dpz.Core.WebMore.Models;

public class SendComment
{
    /// <summary>
    /// 评论类型
    /// </summary>
    [Required(ErrorMessage = "comment node is empty.")]
    [JsonConverter(typeof(EnumConverter<CommentNode>))]
    public CommentNode? Node { get; set; }

    /// <summary>
    /// 关联
    /// </summary>
    [
        Required(ErrorMessage = "relation is empty."),
        MaxLength(500, ErrorMessage = "relation max length 20.")
    ]
    public string? Relation { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    [Required(ErrorMessage = "昵称不能为空"), MaxLength(200, ErrorMessage = "昵称最大长度为200")]
    public string? NickName { get; set; }

    /// <summary>
    /// 邮箱地址
    /// </summary>
    [
        Required(ErrorMessage = "请填写邮箱地址"),
        EmailAddress(ErrorMessage = "请填写正确的邮箱地址"),
        MaxLength(200, ErrorMessage = "邮箱地址最大长度为200")
    ]
    public string? Email { get; set; }

    /// <summary>
    /// 回复内容
    /// </summary>
    [
        Required(ErrorMessage = "请输入回复内容"),
        MaxLength(2000, ErrorMessage = "回复内容最大长度为2000")
    ]
    public string? CommentText { get; set; }

    /// <summary>
    /// 个人网站
    /// </summary>
    [
        Url(ErrorMessage = "请填写正确的个人网站地址"),
        MaxLength(200, ErrorMessage = "个人网站最大长度为200")
    ]
    public string? Site { get; set; }

    /// <summary>
    /// 回复ID
    /// </summary>
    [MaxLength(50, ErrorMessage = "reply id max length 15")]
    public string? ReplyId { get; set; }
}
