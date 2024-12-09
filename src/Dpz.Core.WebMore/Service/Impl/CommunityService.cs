using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service.Impl
{
    public class CommunityService:ICommunityService
    {
        private readonly HttpClient _httpClient;

        public CommunityService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }
        
        public async Task<List<PictureModel>> GetBannersAsync()
        {
            return await _httpClient.GetFromJsonAsync<List<PictureModel>>("/api/Community/getBanners");
        }
    }
}