using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service;

public interface ICommunityService
{
    Task<List<PictureRecordModel>> GetBannersAsync();
    
    Task<List<FriendModel>> GetFriendsAsync();
}