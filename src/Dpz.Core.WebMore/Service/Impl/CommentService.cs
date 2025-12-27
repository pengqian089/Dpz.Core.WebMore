using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Dpz.Core.EnumLibrary;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service.Impl;

public class CommentService(HttpClient httpClient) : ICommentService
{
    public async Task<IPagedList<CommentModel>> GetPageAsync(
        CommentNode node,
        string relation,
        int pageIndex = 1,
        int pageSize = 10
    )
    {
        var parameter = new Dictionary<string, string>
        {
            { nameof(pageIndex), pageIndex.ToString() },
            { nameof(pageSize), pageSize.ToString() },
            { nameof(node), node.ToString() },
            { nameof(relation), relation },
        };
        return await httpClient.ToPagedListAsync<CommentModel>("/api/Comment/page", parameter);
    }

    public async Task<ResultInformation<IPagedList<CommentModel>>> SendAsync(
        SendComment comment,
        int pageSize = 5
    )
    {
        var response = await httpClient.PostAsync(
            $"/api/Comment?pageSize={pageSize}",
            JsonContent.Create(comment)
        );

        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            var message = await response.Content.ReadAsStringAsync();
            return ResultInformation<IPagedList<CommentModel>>.ToFail(message);
        }

        var serializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var result =
            await response.Content.ReadFromJsonAsync<List<CommentModel>>(serializerOptions) ?? [];

        response.Headers.TryGetValues("X-Pagination", out var pageInformation);
        var pagination =
            JsonSerializer.Deserialize<Pagination>(
                pageInformation?.FirstOrDefault() ?? "{}",
                serializerOptions
            ) ?? new Pagination();

        var page = new PagedList<CommentModel>(
            result,
            pagination.CurrentPage,
            pagination.PageSize,
            pagination.TotalCount
        );

        return ResultInformation<IPagedList<CommentModel>>.ToSuccess(page);
    }
}
