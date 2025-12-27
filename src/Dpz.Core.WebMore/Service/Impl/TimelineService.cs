using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service.Impl;

public class TimelineService(HttpClient httpClient) : ITimelineService
{
    public async Task<List<TimelineModel>> GetTimelineAsync()
    {
        var result = await httpClient.GetFromJsonAsync<List<TimelineModel>>("/api/Timeline");
        return result ?? [];
    }
}
