﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;
using Microsoft.Extensions.Logging;

namespace Dpz.Core.WebMore.Service.Impl
{
    public class ArticleService:IArticleService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ArticleService> _logger;

        public ArticleService(
            HttpClient httpClient,
            ILogger<ArticleService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }
        public async Task<IPagedList<ArticleMiniModel>> GetPageAsync(int pageIndex, int pageSize, string tags, string title)
        {
            var parameter = new Dictionary<string, string>
            {
                {nameof(pageIndex) , pageIndex.ToString() },
                {nameof(pageSize) , pageSize.ToString() },
                {nameof(tags) , tags },
                {nameof(title) , title },
            };
            return await _httpClient.ToPagedListAsync<ArticleMiniModel>("/api/Article", parameter);
        }

        public async Task<ArticleModel> GetArticleAsync(string id)
        {
            try
            {
                return await _httpClient.GetFromJsonAsync<ArticleModel>($"/api/Article/{id}", new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch (Exception e)
            {
                _logger.LogWarning(e,"未获取到文章详情");
                return null;
            }
        }

        public async Task<string[]> GetTagsAsync()
        {
            return await _httpClient.GetFromJsonAsync<string[]>("api/Article/tags/all");
        }

        public async Task<List<ArticleMiniModel>> GetViewTopAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<ArticleMiniModel>>("/api/Article/topView");
        }

        public async Task<List<ArticleMiniModel>> GetLikeAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<ArticleMiniModel>>("/api/Article/like");
        }

        public async Task<List<ArticleMiniModel>> GetNewsAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<ArticleMiniModel>>("/api/Article/news");
        }
    }
}