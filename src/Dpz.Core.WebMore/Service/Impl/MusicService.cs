using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service.Impl;

public class MusicService(HttpClient httpClient) : IMusicService
{
    public async Task<IPagedList<MusicModel>> GetMusicPageAsync(int pageIndex, int pageSize)
    {
        var parameter = new Dictionary<string, string>
        {
            { nameof(pageIndex), pageIndex.ToString() },
            { nameof(pageSize), pageSize.ToString() },
        };
        return await httpClient.ToPagedListAsync<MusicModel>("/api/Music", parameter);
    }
}
