using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service.Impl;

public class CommunityService(HttpClient httpClient) : ICommunityService
{
    public async Task<List<PictureModel>> GetBannersAsync()
    {
        return await httpClient.GetFromJsonAsync<List<PictureModel>>("/api/Community/getBanners")
            ?? [];
    }

    public async Task<List<FriendModel>> GetFriendsAsync()
    {
        return await httpClient.GetFromJsonAsync<List<FriendModel>>("/api/Community/friends") ?? [];
    }
}
