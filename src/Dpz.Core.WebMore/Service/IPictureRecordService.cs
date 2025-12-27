using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service;

public interface IPictureRecordService
{
    Task<IPagedList<PictureRecordModel>> GetPagesAsync(
        List<string>? tags,
        string? description,
        int pageIndex = 1,
        int pageSize = 20,
        string? account = null
    );
}
