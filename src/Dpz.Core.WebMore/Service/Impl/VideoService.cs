using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service.Impl;

public class VideoService(HttpClient httpClient) : IVideoService
{
    public async Task<List<VideoModel>> GetVideosAsync()
    {
        return await httpClient.GetFromJsonAsync<List<VideoModel>>("/api/Video") ?? [];
    }
}

