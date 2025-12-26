using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service.Impl;

public class PictureRecordService(HttpClient httpClient) : IPictureRecordService
{
    public async Task<IPagedList<PictureRecordModel>> GetPagesAsync(
        List<string>? tags,
        string? description,
        int pageIndex = 1,
        int pageSize = 20,
        string? account = null
    )
    {
        var parameter = new List<KeyValuePair<string, string>>
        {
            new(nameof(pageIndex), pageIndex.ToString()),
            new(nameof(pageSize), pageSize.ToString()),
            new(nameof(description), description ?? ""),
            new(nameof(account), account ?? ""),
        };

        if (tags is { Count: > 0 })
        {
            foreach (var tag in tags)
            {
                parameter.Add(new KeyValuePair<string, string>(nameof(tags), tag));
            }
        }

        var result = await httpClient.ToPagedListAsync<PictureRecordModel>(
            "/api/Picture",
            parameter
        );
        return result;
    }
}
