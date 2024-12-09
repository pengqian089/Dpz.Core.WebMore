using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using AngleSharp;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service.Impl
{
    public class MumbleService:IMumbleService
    {
        private readonly HttpClient _httpClient;

        public MumbleService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        public async Task<IPagedList<MumbleModel>> GetPageAsync(int pageIndex, int pageSize, string content)
        {
            var parameter = new Dictionary<string, string>
            {
                {nameof(pageIndex) , pageIndex.ToString() },
                {nameof(pageSize) , pageSize.ToString() },
                {nameof(content) , content },
            };
            var result = await _httpClient.ToPagedListAsync<MumbleModel>("/api/Mumble", parameter);
            return result;
        }

        public async Task<MumbleModel> GetMumbleAsync(string id)
        {
            return await _httpClient.GetFromJsonAsync<MumbleModel>($"/api/Mumble/{id}", new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        public async Task<MumbleModel> LikeAsync(string id)
        {
            var response = await _httpClient.PostAsync($"/api/Mumble/like/{id}", new StringContent(""));
            return await response.Content.ReadFromJsonAsync<MumbleModel>();
        }

        public async Task<IPagedList<MumbleCommentModel>> GetCommentPageAsync(string id, int pageIndex, int pageSize)
        {
            var parameter = new Dictionary<string, string>
            {
                {nameof(pageIndex) , pageIndex.ToString() },
                {nameof(pageSize) , pageSize.ToString() },
                {"content" , "" },
            };
            return await _httpClient.ToPagedListAsync<MumbleCommentModel>($"/api/Mumble/comment/{id}", parameter);
        }
    }
}