using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service
{
    public interface IMusicService
    {
        /// <summary>
        /// 获取音乐
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        Task<IPagedList<MusicModel>> GetMusicPageAsync(int pageIndex, int pageSize);
    }
}