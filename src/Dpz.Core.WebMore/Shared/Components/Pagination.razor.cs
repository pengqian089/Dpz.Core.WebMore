using System;
using System.Collections.Generic;
using Dpz.Core.WebMore.Helper;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Shared.Components;

public partial class Pagination
{
    /// <summary>
    /// 分页数据源
    /// </summary>
    [Parameter]
    [EditorRequired]
    public required IPagedList Source { get; set; }

    /// <summary>
    /// 链接生成函数
    /// </summary>
    [Parameter]
    public Func<int, string>? LinkGenerator { get; set; }

    /// <summary>
    /// 链接模板，使用 {0} 替换页码，例如 "/article/list/{0}"
    /// </summary>
    [Parameter]
    public string? LinkTemplate { get; set; }

    /// <summary>
    /// 显示的页码数量，默认为5
    /// </summary>
    [Parameter]
    public int VisiblePageCount { get; set; } = 5;

    private int CurrentPage => Source.CurrentPageIndex;
    private int TotalPages => Source.TotalPageCount;

    private string GetUrl(int page)
    {
        if (LinkGenerator != null)
        {
            return LinkGenerator(page);
        }

        if (!string.IsNullOrEmpty(LinkTemplate))
        {
            try
            {
                return string.Format(LinkTemplate, page);
            }
            catch
            {
                return LinkTemplate.Replace("{0}", page.ToString());
            }
        }

        return "javascript:void(0)";
    }

    private IEnumerable<int> GetVisiblePages()
    {
        var total = TotalPages;
        var current = CurrentPage;
        var count = VisiblePageCount;

        if (total <= count)
        {
            for (var i = 1; i <= total; i++)
            {
                yield return i;
            }
            yield break;
        }

        var start = current - count / 2;
        var end = current + count / 2;

        if (start < 1)
        {
            end += (1 - start);
            start = 1;
        }

        if (end > total)
        {
            start -= (end - total);
            if (start < 1)
            {
                start = 1;
            }
            end = total;
        }

        for (var i = start; i <= end; i++)
        {
            yield return i;
        }
    }
}
