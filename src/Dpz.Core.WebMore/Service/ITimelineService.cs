using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service
{
    public interface ITimelineService
    {
        /// <summary>
        /// 获取时间轴
        /// </summary>
        /// <returns></returns>
        Task<List<TimelineModel>> GetTimelineAsync();
    }
}