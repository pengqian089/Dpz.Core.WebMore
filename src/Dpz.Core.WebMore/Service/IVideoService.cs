using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service;

public interface IVideoService
{
    Task<List<VideoModel>> GetVideosAsync();
}

