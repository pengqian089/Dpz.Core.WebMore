using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Web;
using AngleSharp;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;
using Markdig;

namespace Dpz.Core.WebMore.Service.Impl
{
    public class TopicService : ITopicService
    {
        private readonly HttpClient _httpClient;

        public TopicService(
            HttpClient httpClient
        )
        {
            _httpClient = httpClient;
        }

        private async Task<string> HandleContent(IBrowsingContext context, string content)
        {
            var htmlContent = Markdown.ToHtml(content);
            var document = await context.OpenAsync(y => y.Content(htmlContent));
            var links = document.GetElementsByTagName("a");
            links.ForEach(y =>
            {
                var href = y.GetAttribute("href");
                if (href != null && !href.StartsWith("javascript", StringComparison.CurrentCultureIgnoreCase))
                {
                    var uri = new Uri(href);
                    var parameters = HttpUtility.ParseQueryString(uri.Query);
                    var target = parameters["target"];
                    if (!string.IsNullOrEmpty(target))
                    {
                        y.SetAttribute("href", target);
                    }
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
                    if (src == $"{Program.CdnBaseAddress}/core/loaders/bars.svg") return;
                    y.SetAttribute("class","lazy");
                    y.SetAttribute("src", $"{Program.CdnBaseAddress}/core/loaders/bars.svg");
                    y.SetAttribute("data-src", $"{Program.WebHost}{src}");
                }
                else
                {
                    y.SetAttribute("src" ,$"{Program.CdnBaseAddress}/core/images/notfound.png");
                }
                
            });
            return document.Body?.InnerHtml ?? "";
        }

        public async Task<IPagedList<TopicModel>> GetTopicPageAsync(int pageIndex, int pageSize)
        {
            var parameter = new Dictionary<string, string>
            {
                {nameof(pageIndex), pageIndex.ToString()},
                {nameof(pageSize), pageSize.ToString()},
            };
            var result = await _httpClient.ToPagedListAsync<TopicModel>("/api/Topic", parameter);
            var context = BrowsingContext.New(Configuration.Default);
            await result.ForEachAsync(async x =>
            {
                x.Content = await HandleContent(context, x.Content);
            });
            return result;
        }

        public async Task<TopicModel> GetTopicAsync(string id)
        {
            var model = await _httpClient.GetFromJsonAsync<TopicModel>($"/api/Topic/{id}");
            if (model == null) return null;
            var context = BrowsingContext.New(Configuration.Default);
            model.Content = await HandleContent(context, model.Content);
            return model;
        }

        public async Task<IPagedList<TopicCommentModel>> GetTopicCommentPageAsync(string id, int pageIndex, int pageSize)
        {
            var parameter = new Dictionary<string, string>
            {
                {nameof(pageIndex), pageIndex.ToString()},
                {nameof(pageSize), pageSize.ToString()},
            };
            var result = await _httpClient.ToPagedListAsync<TopicCommentModel>($"/api/Topic/comment/{id}", parameter);
            var context = BrowsingContext.New(Configuration.Default);
            await result.ForEachAsync(async x => x.Content = await HandleContent(context, x.Content));
            return result;
        }
    }
}