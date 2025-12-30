using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dpz.Core.WebMore.Models;

public class ArticleSearchResultModel : ArticleModel
{
    /// <summary>
    /// 标题搜索结果
    /// </summary>
    public List<SearchResultModel> TitleSearchResult { get; set; } = [];

    /// <summary>
    /// 内容搜索结果 --> Markdown
    /// </summary>
    public List<SearchResultModel> ContentSearchResult { get; set; } = [];

    public string HighlightTitle()
    {
        if (TitleSearchResult.Count == 0)
        {
            return Title;
        }
        return HighlightText(TitleSearchResult, Title);
    }

    public string HighlightContent(bool readAll = false)
    {
        if (ContentSearchResult.Count == 0)
        {
            return "";
        }
        return HighlightText(ContentSearchResult, Markdown, readAll);
    }

    private static string HighlightText(
        List<SearchResultModel> searchResults,
        string text,
        bool readAll = false
    )
    {
        var highlightedText = new StringBuilder();
        using var reader = new StringReader(text);
        var lineNumber = 1;

        var groupedResults = searchResults
            .GroupBy(x => x.LineNumber)
            .ToDictionary(x => x.Key, x => x.ToList());

        while (reader.ReadLine() is { } line)
        {
            if (groupedResults.TryGetValue(lineNumber, out var matchesInLine))
            {
                // 找出当前行中所有的 Markdown 链接/图片 URL 部分
                // 匹配 [...](url) 或 ![...](url) 中的 url 部分
                var unsafeRanges = new List<(int Start, int End)>();
                var linkMatches = System.Text.RegularExpressions.Regex.Matches(
                    line,
                    @"!?\[.*?\]\((.*?)\)"
                );
                foreach (System.Text.RegularExpressions.Match m in linkMatches)
                {
                    // Group[1] 是括号内的 URL 部分
                    if (m.Groups.Count > 1)
                    {
                        var g = m.Groups[1];
                        unsafeRanges.Add((g.Index, g.Index + g.Length));
                    }
                }

                // 按照 StartIndex 排序，确保从左到右处理匹配项
                var sortedMatches = matchesInLine.OrderBy(x => x.StartIndex).ToList();
                // 按照匹配的结果高亮文本
                var highlightedLine = line;
                // 跟踪插入标记后的偏移量
                var offset = 0;

                foreach (var result in sortedMatches)
                {
                    // 检查匹配项是否在不安全的范围内（如 URL 中）
                    // 如果匹配项的起始或结束位置在 URL 范围内，则跳过高亮
                    if (
                        unsafeRanges.Any(r =>
                            result.StartIndex >= r.Start && result.EndIndex < r.End
                        )
                    )
                    {
                        continue;
                    }

                    // 计算调整后的索引
                    var adjustedStartIndex = result.StartIndex + offset;
                    var adjustedEndIndex = result.EndIndex + offset;

                    // 获取匹配前后的字符串
                    var beforeMatch = highlightedLine[..adjustedStartIndex];
                    var match = highlightedLine.Substring(
                        adjustedStartIndex,
                        adjustedEndIndex - adjustedStartIndex + 1
                    );
                    var afterMatch = highlightedLine[(adjustedEndIndex + 1)..];

                    highlightedLine = $"{beforeMatch}{HighlightTag(match)}{afterMatch}";

                    // 更新偏移量
                    offset += HighlightTag(null).Length;
                }
                // 将高亮后的行加入到最终结果中
                highlightedText.AppendLine(highlightedLine);
            }
            else if (readAll)
            {
                highlightedText.AppendLine(line);
            }
            lineNumber++;
        }

        return highlightedText.ToString();

        string HighlightTag(string? match)
        {
            return $"<mark>{match}</mark>";
        }
    }
}
