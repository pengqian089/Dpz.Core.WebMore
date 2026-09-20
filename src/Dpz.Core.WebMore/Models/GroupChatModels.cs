using System;

namespace Dpz.Core.WebMore.Models;

/// <summary>
/// 群聊消息模型
/// </summary>
public class GroupChatMessage
{
    public string UserId { get; set; } = "";
    public string UserName { get; set; } = "";
    public string Avatar { get; set; } = "";
    public string Message { get; set; } = "";
    public DateTime SendTime { get; set; }
}
