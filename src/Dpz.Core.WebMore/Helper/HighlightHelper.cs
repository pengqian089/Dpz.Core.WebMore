using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Dpz.Core.WebMore.Helper;

public static class HighlightHelper
{
    public static string HighlightKeywords(string text, string keyword)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(keyword))
            return text;

        // 找出所有 Markdown 链接/图片的范围，避免破坏 URL
        // 匹配 [...](url) 或 ![...](url)
        var unsafeRanges = new List<(int Start, int End)>();

        // Match [...](...)
        var linkMatches = Regex.Matches(text, @"!?\[.*?\]\((.*?)\)");
        foreach (Match m in linkMatches)
        {
            // 保护整个 Markdown 链接语法结构，避免插入 HTML 标签破坏渲染
            unsafeRanges.Add((m.Index, m.Index + m.Length));
        }

        // Match HTML tags <...> (prevent breaking existing HTML)
        var htmlMatches = Regex.Matches(text, @"<[^>]+>");
        foreach (Match m in htmlMatches)
        {
            unsafeRanges.Add((m.Index, m.Index + m.Length));
        }

        var sb = new StringBuilder();
        int currentIndex = 0;

        // 查找所有关键词出现的位置
        var keywordMatches = Regex.Matches(text, Regex.Escape(keyword), RegexOptions.IgnoreCase);

        foreach (Match match in keywordMatches)
        {
            // 如果当前匹配项在任何不安全范围内，跳过
            // 这里我们判断匹配项的起始点是否在禁止区域内
            // 只要有一部分重叠就跳过，为了安全起见
            bool isUnsafe = unsafeRanges.Any(r =>
                (match.Index >= r.Start && match.Index < r.End)
                || (match.Index + match.Length > r.Start && match.Index + match.Length <= r.End)
            );

            if (isUnsafe)
            {
                continue;
            }

            // 添加当前匹配项之前的内容
            if (match.Index > currentIndex)
            {
                sb.Append(text[currentIndex..match.Index]);
            }

            // 添加高亮标记
            sb.Append($"<mark>{match.Value}</mark>");

            currentIndex = match.Index + match.Length;
        }

        // 添加剩余内容
        if (currentIndex < text.Length)
        {
            sb.Append(text[currentIndex..]);
        }

        return sb.ToString();
    }
}
