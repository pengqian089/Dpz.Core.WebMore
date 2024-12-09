using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service.Impl;

public class BookmarkService(HttpClient httpClient) : IBookmarkService
{
    private static Hashtable _hashtable = new Hashtable();

    public async Task<List<BookmarkModel>> GetBookmarksAsync(string? title, ICollection<string?>? categories)
    {
        const string cachePrefix = "bookmark";
        var (cacheKey, parameterJoin) = GetParameters(cachePrefix, title, categories);
        return await GetDataAsync<BookmarkModel>(cacheKey, "/api/Bookmark" + parameterJoin);
    }

    public async Task<List<string>> GetCategoriesAsync()
    {
        const string cacheKey = "all-categories";
        return await GetDataAsync<string>(cacheKey, "/api/Bookmark/categories");
    }

    public async Task<List<string>> SearchAsync(string? title, ICollection<string?>? categories)
    {
        const string cachePrefix = "search";
        var (cacheKey, parameterJoin) = GetParameters(cachePrefix, title, categories);
        return await GetDataAsync<string>(cacheKey, "/api/Bookmark/search" + parameterJoin);
    }

    private static (string cacheKey, string parameterJoin) GetParameters(
        string cachePrefix, 
        string? title,
        ICollection<string?>? categories)
    {
        var parameters = new List<string>();
        if (!string.IsNullOrEmpty(title))
        {
            parameters.Add($"title={WebUtility.UrlEncode(title)}");
        }

        if (categories is { Count: > 0 })
        {
            parameters.AddRange(categories
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct()
                .Select(category => $"categories={WebUtility.UrlEncode(category)}"));
        }

        var parameterJoin = parameters.Count > 0 ? "?" + string.Join("&", parameters) : "";
        var cacheKey = cachePrefix + parameterJoin;
        return (cacheKey, parameterJoin);
    }

    private async ValueTask<List<T>> GetDataAsync<T>(string cacheKey, string requestUri)
    {
        if (_hashtable.ContainsKey(cacheKey) && _hashtable[cacheKey] is List<T> result)
        {
            return result;
        }

        var data = await httpClient.GetFromJsonAsync<List<T>>(requestUri) ?? [];
        _hashtable[cacheKey] = data;
        return data;
    }
}