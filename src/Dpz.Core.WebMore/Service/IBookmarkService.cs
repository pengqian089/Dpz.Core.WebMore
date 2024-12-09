using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service;

public interface IBookmarkService
{
    /// <summary>
    /// 获取书签
    /// </summary>
    /// <param name="title"></param>
    /// <param name="categories"></param>
    /// <returns></returns>
     Task<List<BookmarkModel>> GetBookmarksAsync(string? title,ICollection<string?>? categories);

    /// <summary>
    /// 获取所有分类
    /// </summary>
    /// <returns></returns>
    Task<List<string>> GetCategoriesAsync();

    /// <summary>
    /// 搜索书签
    /// </summary>
    /// <param name="title"></param>
    /// <param name="categories"></param>
    /// <returns></returns>
    Task<List<string>> SearchAsync(string? title,ICollection<string?>? categories);
}