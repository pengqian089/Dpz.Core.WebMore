using System;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Shared.Components;

public partial class ShowDateTime : ComponentBase
{
    [Parameter]
    [EditorRequired]
    public required DateTime DateTime { get; set; }

    [Parameter]
    public string? Class { get; set; }

    private DateTime _lastDateTime;
    private string? _lastClass;

    protected override bool ShouldRender()
    {
        // 只有当参数改变时才重新渲染
        if (_lastDateTime != DateTime || _lastClass != Class)
        {
            _lastDateTime = DateTime;
            _lastClass = Class;
            return true;
        }
        return false;
    }

    private string UpdateTimeTag()
    {
        var now = System.DateTime.Now;
        var diff = (now - DateTime).TotalSeconds;

        return diff switch
        {
            < 60 => "刚刚",
            < 3600 => $"{Math.Ceiling(diff / 60)} 分钟前",
            < 86400 => $"{Math.Ceiling(diff / 3600)} 小时前",
            // 30 days
            < 2592000 => $"{Math.Ceiling(diff / 86400)} 天前",
            // 365 days
            < 31536000 => $"{Math.Ceiling(diff / 2592000)} 个月前",
            _ => $"{Math.Ceiling(diff / 31536000)} 年前",
        };
    }
}
