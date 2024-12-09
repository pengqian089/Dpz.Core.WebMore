using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service
{
    public interface IArticleService
    {
        /// <summary>
        /// 分页获取文章列表
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="tags"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        Task<IPagedList<ArticleMiniModel>> GetPageAsync(int pageIndex,int pageSize,string tags,string title);

        /// <summary>
        /// 获取文章详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        Task<ArticleModel> GetArticleAsync(string id);

        /// <summary>
        /// 获取所有标签
        /// </summary>
        /// <returns></returns>
        Task<string[]> GetTagsAsync();

        /// <summary>
        /// 获取查看最多的文章
        /// </summary>
        /// <returns></returns>
        Task<List<ArticleMiniModel>> GetViewTopAsync();

        /// <summary>
        /// 猜你喜欢
        /// </summary>
        /// <returns></returns>
        Task<List<ArticleMiniModel>> GetLikeAsync();

        /// <summary>
        /// 最新文章
        /// </summary>
        /// <returns></returns>
        Task<List<ArticleMiniModel>> GetNewsAsync();
    }
}