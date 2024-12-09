using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service
{
    public interface ITopicService
    {
        /// <summary>
        /// 获取热榜分页数据
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<IPagedList<TopicModel>> GetTopicPageAsync(int pageIndex,int pageSize);

        /// <summary>
        /// 获取提问详情，并获得第一页回答数据
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<TopicModel> GetTopicAsync(string id);

        /// <summary>
        /// 获取回答分页数据
        /// </summary>
        /// <param name="id"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<IPagedList<TopicCommentModel>> GetTopicCommentPageAsync(string id, int pageIndex, int pageSize);
    }
}