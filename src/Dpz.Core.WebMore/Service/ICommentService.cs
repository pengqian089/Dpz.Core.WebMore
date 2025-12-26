using System.Threading.Tasks;
using Dpz.Core.EnumLibrary;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service;

public interface ICommentService
{
    Task<IPagedList<CommentModel>> GetPageAsync(
        CommentNode node,
        string relation,
        int pageIndex = 1,
        int pageSize = 10
    );

    Task<IPagedList<CommentModel>> SendAsync(SendComment comment, int pageSize = 5);
}
