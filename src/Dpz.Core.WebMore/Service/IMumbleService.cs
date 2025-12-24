using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service;

public interface IMumbleService
{
    /// <summary>
    /// 碎碎念分页获取
    /// </summary>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <param name="content"></param>
    /// <returns></returns>
    Task<IPagedList<MumbleModel>> GetPageAsync(int pageIndex, int pageSize, string content);

    /// <summary>
    /// 获取碎碎念详情
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<MumbleModel?> GetMumbleAsync(string id);

    /// <summary>
    /// 点赞
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    Task<MumbleModel?> LikeAsync(string id);

    /// <summary>
    /// 获取碎碎念回复
    /// </summary>
    /// <param name="id"></param>
    /// <param name="pageIndex"></param>
    /// <param name="pageSize"></param>
    /// <returns></returns>
    Task<IPagedList<MumbleCommentModel>> GetCommentPageAsync(string id, int pageIndex, int pageSize);

    /// <summary>
    /// 获取碎碎念历史
    /// </summary>
    /// <returns></returns>
    Task<List<MumbleModel>> GetHistories();
}