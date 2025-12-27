using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Service;

public interface ICodeService
{
    Task<CodeNoteTree> GetTreeAsync(params string[]? path);

    Task<CodeNoteTree> SearchAsync(string? keyword);
}
