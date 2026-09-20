using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AngleSharp;
using AngleSharp.Dom;
using Dpz.Core.WebMore.Models;
using Markdig;

namespace Dpz.Core.WebMore.Helper;

/// <summary>
/// Markdown 渲染器：转换为 HTML 并处理链接与图片
/// </summary>
public static class MarkdownRenderer
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAutoLinks()
        .UsePipeTables()
        .UseTaskLists()
        .UseEmphasisExtras()
        .UseFooters()
        .UseCitations()
        .UseMathematics()
        .UseAutoIdentifiers()
        .Build();

    /// <summary>
    /// 将 Markdown 渲染为 HTML
    /// </summary>
    /// <param name="markdown">Markdown 内容</param>
    /// <param name="stripImages">是否提取并移除图片（用于九宫格展示）</param>
    /// <returns>HTML 内容与提取出的图片列表</returns>
    public static async Task<(string Html, List<MumbleImageModel> Images)> RenderAsync(
        string markdown,
        bool stripImages = false
    )
    {
        var images = new List<MumbleImageModel>();
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return (string.Empty, images);
        }

        var html = Markdig.Markdown.ToHtml(markdown, Pipeline);
        var context = BrowsingContext.New(Configuration.Default);
        var document = await context.OpenAsync(y => y.Content(html));

        OpenLinksInNewTab(document);
        if (stripImages)
        {
            images = ExtractImages(document);
        }
        else
        {
            RewriteImagesForLazyLoad(document);
        }

        return (document.Body?.InnerHtml ?? string.Empty, images);
    }

    /// <summary>
    /// 给链接添加新标签页打开属性
    /// </summary>
    private static void OpenLinksInNewTab(IDocument document)
    {
        var links = document.GetElementsByTagName("a");
        links.ForEach(y =>
        {
            var href = y.GetAttribute("href");
            if (
                href != null
                && !href.StartsWith("javascript", StringComparison.CurrentCultureIgnoreCase)
            )
            {
                y.SetAttribute("target", "_blank");
            }
        });
    }

    /// <summary>
    /// 提取并移除图片元素
    /// </summary>
    private static List<MumbleImageModel> ExtractImages(IDocument document)
    {
        var images = new List<MumbleImageModel>();
        var elements = document.GetElementsByTagName("img").ToList();
        var index = 1;

        foreach (var element in elements)
        {
            var src = element.GetAttribute("src")?.Trim();
            if (string.IsNullOrWhiteSpace(src) || IsPlaceholder(src))
            {
                continue;
            }

            var alt = element.GetAttribute("alt");
            if (string.IsNullOrWhiteSpace(alt))
            {
                alt = $"碎碎念图片 {index}";
            }

            images.Add(new MumbleImageModel { Url = src, Alt = alt });
            element.Remove();
            index++;
        }

        return images;
    }

    /// <summary>
    /// 将正文图片重写为懒加载占位
    /// </summary>
    private static void RewriteImagesForLazyLoad(IDocument document)
    {
        var images = document.GetElementsByTagName("img");
        images.ForEach(y =>
        {
            var src = y.GetAttribute("src");
            if (string.IsNullOrWhiteSpace(src) || IsPlaceholder(src))
            {
                return;
            }

            y.SetAttribute("data-src", src);
            y.SetAttribute("class", "lazy");
            y.SetAttribute("src", $"{Program.LibraryHost}/loaders/oval.svg");
        });
    }

    private static bool IsPlaceholder(string src)
    {
        return string.Equals(
            src,
            $"{Program.LibraryHost}/loaders/oval.svg",
            StringComparison.OrdinalIgnoreCase
        );
    }
}
