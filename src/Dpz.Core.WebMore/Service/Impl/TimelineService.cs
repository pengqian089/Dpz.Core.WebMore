using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using AngleSharp;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;
using Markdig;

namespace Dpz.Core.WebMore.Service.Impl
{
    public class TimelineService:ITimelineService
    {
        private readonly HttpClient _httpClient;

        public TimelineService(
            HttpClient httpClient
            )
        {
            _httpClient = httpClient;
        }
        
        public async Task<List<TimelineModel>> GetTimelineAsync()
        {
            var result = await _httpClient.GetFromJsonAsync<List<TimelineModel>>("/api/Timeline");
            var context = BrowsingContext.New(Configuration.Default);
            await result.ForEachAsync(async x =>
            {
                if (!string.IsNullOrEmpty(x.Content))
                {
                    var htmlContent = Markdown.ToHtml(x.Content);
                    var document = await context.OpenAsync(y => y.Content(htmlContent));
                    var links = document.GetElementsByTagName("a");
                    links.ForEach(y =>
                    {
                        var href = y.GetAttribute("href");
                        if (href != null && !href.StartsWith("javascript", StringComparison.CurrentCultureIgnoreCase))
                        {
                            y.SetAttribute("target", "_blank");
                            if (href.StartsWith("/"))
                            {
                                y.SetAttribute("href",$"{Program.WebHost}{href}");
                            }
                        }
                    });
                    var images = document.GetElementsByTagName("img");
                    images.ForEach(y =>
                    {
                        var src = y.GetAttribute("src");
                        if (!string.IsNullOrEmpty(src) && src.StartsWith("/"))
                        {
                            y.SetAttribute("src",$"{Program.WebHost}{src}");
                        }
                    });
                    x.Content = document.Body?.InnerHtml ?? "";
                }
            });
            return result;
        }
    }
}