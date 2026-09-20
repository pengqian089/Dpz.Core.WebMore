using System;
using System.Collections.Generic;
using Dpz.Core.WebMore.Models;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Shared.Components;

public partial class MumbleGallery : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public IReadOnlyList<MumbleImageModel> Images { get; set; } = [];

    /// <summary>
    /// 画廊标识，用于 PhotoSwipe 的 hash 导航
    /// </summary>
    [Parameter]
    public string GalleryId { get; set; } = "";

    /// <summary>
    /// 容器自定义样式类
    /// </summary>
    [Parameter]
    public string CssClass { get; set; } = "mumble-card__gallery";

    /// <summary>
    /// 预览区最多显示的真实图片数量，超出时显示 +N
    /// </summary>
    [Parameter]
    public int? PreviewImageCount { get; set; }

    private int PreviewCount
    {
        get
        {
            if (Images.Count == 0)
            {
                return 0;
            }

            var count = PreviewImageCount ?? (Images.Count > 9 ? 8 : Images.Count);
            return Math.Clamp(count, 1, Images.Count);
        }
    }

    private bool ShowMore => Images.Count > PreviewCount;

    private int GridCount => Math.Min(PreviewCount + (ShowMore ? 1 : 0), 9);
}
