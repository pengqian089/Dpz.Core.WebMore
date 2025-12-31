using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Pages.Tools;

public partial class RegexTest : ComponentBase
{
    private string _pattern = string.Empty;
    private string _testText = string.Empty;
    private bool _ignoreCase = false;
    private bool _multiline = false;
    private bool _singleline = false;
    private bool _explicitCapture = false;
    private string _errorMessage = string.Empty;
    private string _highlightedText = string.Empty;
    private RegexMatchResult? _matchResult;
    private HashSet<int> _expandedMatches = [];
    private bool _expandAll = false;
    private bool _showExamples = false;
    private bool _showReference = true;
    private List<RegexExample> _examples = [];

    protected override void OnInitialized()
    {
        InitializeExamples();
    }

    protected override void OnParametersSet()
    {
        PerformMatch();
    }

    private void InitializeExamples()
    {
        _examples =
        [
            new RegexExample
            {
                Name = "邮箱地址",
                Pattern = @"^[\w\.-]+@[\w\.-]+\.\w+$",
                TestText = "example@email.com\ntest.user@domain.co.uk\ninvalid@email",
            },
            new RegexExample
            {
                Name = "手机号码",
                Pattern = @"^1[3-9]\d{9}$",
                TestText = "13812345678\n15987654321\n12345678901",
            },
            new RegexExample
            {
                Name = "IP地址",
                Pattern = @"^((25[0-5]|2[0-4]\d|[01]?\d\d?)\.){3}(25[0-5]|2[0-4]\d|[01]?\d\d?)$",
                TestText = "192.168.1.1\n255.255.255.0\n999.999.999.999",
            },
            new RegexExample
            {
                Name = "URL",
                Pattern = @"^https?://[\w\.-]+\.\w+(/[\w\.-]*)*/?$",
                TestText =
                    "https://www.example.com\nhttp://test.com/path/to/page\nftp://invalid.url",
            },
            new RegexExample
            {
                Name = "身份证号",
                Pattern = @"^\d{17}[\dXx]$",
                TestText = "110101199001011234\n12345678901234567X\n123456789012345",
            },
            new RegexExample
            {
                Name = "日期(YYYY-MM-DD)",
                Pattern = @"^\d{4}-(0[1-9]|1[0-2])-(0[1-9]|[12]\d|3[01])$",
                TestText = "2024-01-15\n2024-12-31\n2024-13-45",
            },
            new RegexExample
            {
                Name = "时间(HH:MM:SS)",
                Pattern = @"^([01]\d|2[0-3]):[0-5]\d:[0-5]\d$",
                TestText = "12:30:45\n23:59:59\n25:61:61",
            },
            new RegexExample
            {
                Name = "邮政编码",
                Pattern = @"^\d{6}$",
                TestText = "100000\n518000\n12345",
            },
            new RegexExample
            {
                Name = "HTML标签",
                Pattern = @"<(\w+)[^>]*>.*?</\1>",
                TestText = "<div>内容</div>\n<p class=\"text\">段落</p>\n<span>不匹配",
            },
            new RegexExample
            {
                Name = "十六进制颜色",
                Pattern = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$",
                TestText = "#FF0000\n#f0f\n#GGGGGG",
            },
        ];
    }

    private void LoadExample(RegexExample example)
    {
        _pattern = example.Pattern;
        _testText = example.TestText;
        PerformMatch();
    }

    private void ToggleExamples()
    {
        _showExamples = !_showExamples;
    }

    private void ToggleReference()
    {
        _showReference = !_showReference;
    }

    private void ClearPattern()
    {
        _pattern = string.Empty;
        _errorMessage = string.Empty;
        _matchResult = null;
        _highlightedText = string.Empty;
        _expandedMatches.Clear();
    }

    private void PerformMatch()
    {
        _errorMessage = string.Empty;
        _matchResult = null;
        _highlightedText = string.Empty;

        if (string.IsNullOrEmpty(_pattern) || string.IsNullOrEmpty(_testText))
        {
            return;
        }

        try
        {
            var options = RegexOptions.None;
            if (_ignoreCase)
            {
                options |= RegexOptions.IgnoreCase;
            }

            if (_multiline)
            {
                options |= RegexOptions.Multiline;
            }

            if (_singleline)
            {
                options |= RegexOptions.Singleline;
            }

            if (_explicitCapture)
            {
                options |= RegexOptions.ExplicitCapture;
            }

            var regex = new Regex(_pattern, options);
            var matches = regex.Matches(_testText);

            var matchInfos = new List<RegexMatchInfo>();
            var allGroups = new HashSet<string>();

            foreach (Match match in matches)
            {
                var groups = new List<RegexGroupInfo>();
                foreach (Group group in match.Groups)
                {
                    var groupName = regex.GroupNameFromNumber(group.Index);
                    if (groupName != "0")
                    {
                        allGroups.Add(groupName);
                    }

                    groups.Add(
                        new RegexGroupInfo
                        {
                            Name = groupName,
                            Value = group.Value,
                            Index = group.Index,
                            Length = group.Length,
                        }
                    );
                }

                matchInfos.Add(
                    new RegexMatchInfo
                    {
                        Value = match.Value,
                        Index = match.Index,
                        Length = match.Length,
                        Groups = groups,
                    }
                );
            }

            _matchResult = new RegexMatchResult
            {
                MatchCount = matches.Count,
                GroupCount = allGroups.Count,
                Matches = matchInfos,
                HasError = false,
            };

            GenerateHighlightedText(matchInfos);
        }
        catch (ArgumentException ex)
        {
            _errorMessage = $"正则表达式错误: {ex.Message}";
            _matchResult = new RegexMatchResult { HasError = true };
        }
        catch (Exception ex)
        {
            _errorMessage = $"发生错误: {ex.Message}";
            _matchResult = new RegexMatchResult { HasError = true };
        }
    }

    private void GenerateHighlightedText(List<RegexMatchInfo> matches)
    {
        if (!matches.Any())
        {
            _highlightedText =
                $"<span class=\"regex-test__text-nomatch\">{EscapeHtml(_testText)}</span>";
            return;
        }

        var sb = new StringBuilder();
        var lastIndex = 0;

        foreach (var match in matches.OrderBy(m => m.Index))
        {
            if (match.Index > lastIndex)
            {
                var unmatchedText = _testText.Substring(lastIndex, match.Index - lastIndex);
                sb.Append(
                    $"<span class=\"regex-test__text-nomatch\">{EscapeHtml(unmatchedText)}</span>"
                );
            }

            var matchedText = _testText.Substring(match.Index, match.Length);
            sb.Append(
                $"<span class=\"regex-test__text-match\" title=\"匹配项\">{EscapeHtml(matchedText)}</span>"
            );

            lastIndex = match.Index + match.Length;
        }

        if (lastIndex < _testText.Length)
        {
            var remainingText = _testText.Substring(lastIndex);
            sb.Append(
                $"<span class=\"regex-test__text-nomatch\">{EscapeHtml(remainingText)}</span>"
            );
        }

        _highlightedText = sb.ToString();
    }

    private static string EscapeHtml(string text)
    {
        return text.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;")
            .Replace("\n", "<br/>")
            .Replace(" ", "&nbsp;");
    }

    private void ToggleMatch(int index)
    {
        if (_expandedMatches.Contains(index))
        {
            _expandedMatches.Remove(index);
        }
        else
        {
            _expandedMatches.Add(index);
        }
    }

    private void ToggleAllGroups()
    {
        _expandAll = !_expandAll;
        if (_expandAll)
        {
            if (_matchResult != null)
            {
                _expandedMatches = Enumerable.Range(0, _matchResult.Matches.Count).ToHashSet();
            }
        }
        else
        {
            _expandedMatches.Clear();
        }
    }

    private class RegexMatchResult
    {
        public int MatchCount { get; init; }
        public int GroupCount { get; init; }
        public List<RegexMatchInfo> Matches { get; init; } = [];
        public bool HasError { get; init; }
    }

    private class RegexMatchInfo
    {
        public required string Value { get; init; }
        public required int Index { get; init; }
        public required int Length { get; init; }
        public required List<RegexGroupInfo> Groups { get; init; }
    }

    private class RegexGroupInfo
    {
        public required string Name { get; init; }
        public required string Value { get; init; }
        public required int Index { get; init; }
        public required int Length { get; init; }
    }

    private class RegexExample
    {
        public required string Name { get; init; }
        public required string Pattern { get; init; }
        public required string TestText { get; init; }
    }
}
