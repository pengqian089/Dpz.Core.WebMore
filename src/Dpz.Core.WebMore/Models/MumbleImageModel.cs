namespace Dpz.Core.WebMore.Models;

/// <summary>
/// 碎碎念图片展示模型
/// </summary>
public class MumbleImageModel
{
    /// <summary>
    /// 原图地址
    /// </summary>
    public required string Url { get; set; }

    /// <summary>
    /// 替代文本
    /// </summary>
    public string Alt { get; set; } = "";

    /// <summary>
    /// 图片宽度（像素）
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// 图片高度（像素）
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 是否有可信尺寸
    /// </summary>
    public bool HasSize => Width > 0 && Height > 0;

    /// <summary>
    /// 缩略图地址
    /// </summary>
    public string ThumbnailUrl =>
        Program.IsCdnHost(Url) ? $"{Url}!thumb" : Url;
}
