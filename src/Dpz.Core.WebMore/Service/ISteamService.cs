using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service;

public interface ISteamService
{
    Task<List<SteamModel>> GetSteamLibrariesAsync();

    Task<SteamModel> GetLibraryAsync(int id);
}
