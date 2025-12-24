using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service.Impl;

public class SteamService(HttpClient httpClient) : ISteamService
{
    public async Task<List<SteamModel>> GetSteamLibrariesAsync()
    {
        return await httpClient.GetFromJsonAsync<List<SteamModel>>("/api/Steam") ?? [];
    }

    public Task<SteamModel?> GetLibraryAsync(int id)
    {
        return httpClient.GetFromJsonAsync<SteamModel>($"/api/Steam/{id}");
    }
}
