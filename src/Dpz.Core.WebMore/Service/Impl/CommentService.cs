﻿using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Dpz.Core.EnumLibrary;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service.Impl;

public class CommentService : ICommentService
{
    private readonly HttpClient _httpClient;

    public CommentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<IPagedList<CommentModel>> GetPageAsync(CommentNode node, string relation, int pageIndex = 1,
        int pageSize = 10)
    {
        var parameter = new Dictionary<string, string>
        {
            { nameof(pageIndex), pageIndex.ToString() },
            { nameof(pageSize), pageSize.ToString() },
            { nameof(node), node.ToString() },
            { nameof(relation), relation },
        };
        return await _httpClient.ToPagedListAsync<CommentModel>("/api/Comment/page", parameter);
    }

    public async Task<IPagedList<CommentModel>> SendAsync(SendComment comment, int pageSize = 5)
    {
        var response = await _httpClient.PostAsync($"/api/Comment?pageSize={pageSize}", JsonContent.Create(comment));

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
        var result = await response.Content.ReadFromJsonAsync<List<CommentModel>>(serializerOptions);

        response.Headers.TryGetValues("X-Pagination", out var pageInformation);
        var pagination =
            JsonSerializer.Deserialize<Pagination>(pageInformation?.FirstOrDefault() ?? "{}",
                serializerOptions) ?? new Pagination();

        return new PagedList<CommentModel>(result, pagination.CurrentPage, pagination.PageSize, pagination.TotalCount);
    }
}