using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service.Impl;

public class CodeService(HttpClient httpClient) : ICodeService
{
    private static Dictionary<string, CodeNoteTree?> _cache = new();

    public async Task<CodeNoteTree?> GetTreeAsync(params string[]? path)
    {
        var cacheKey = "CacheKey";
        var parameters = "";
        if (path is not null && path.Length > 0)
        {
            parameters = string.Join("&", path.Select(x => $"path={WebUtility.UrlEncode(x)}"));
            parameters = "?" + parameters;
            cacheKey = string.Join("/", path);
        }

        if (_cache.TryGetValue(cacheKey, out var cacheNode))
        {
            return cacheNode;
        }


        var node = await httpClient.GetFromJsonAsync<CodeNoteTree>("/api/Code" + parameters);
        _cache.Add(cacheKey, node);
        return node;
    }

    public async Task<CodeNoteTree?> SearchAsync(string keyword)
    {
        return await httpClient.GetFromJsonAsync<CodeNoteTree>($"/api/Code/search?keyword={keyword}");
    }
}