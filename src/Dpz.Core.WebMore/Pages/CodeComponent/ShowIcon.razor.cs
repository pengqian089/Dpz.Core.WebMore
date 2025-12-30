using System.Linq;
using Dpz.Core.WebMore.Helper.Icons;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Pages.CodeComponent;

public partial class ShowIcon : ComponentBase
{
    [Parameter]
    public bool IsFolder { get; set; }

    [Parameter]
    public string Filename { get; set; } = "";

    private bool _lastIsFolder;
    private string _lastFilename = "";

    protected override bool ShouldRender()
    {
        // 只有当参数改变时才重新渲染
        if (_lastIsFolder != IsFolder || _lastFilename != Filename)
        {
            _lastIsFolder = IsFolder;
            _lastFilename = Filename;
            return true;
        }
        return false;
    }

    private string MatchIcon()
    {
        var name = Filename.ToLower();
        var languageIcons = MaterialIcon.GetLanguageIcons();
        var fileIcons = MaterialIcon.GetFileIcons();
        var folderIcons = MaterialIcon.GetFolderIcons();

        var folderIcon = folderIcons.FirstOrDefault(x => x.FolderNames.Contains(name));
        if (folderIcon != null)
        {
            return folderIcon.Name;
        }

        var fileIcon = fileIcons.FirstOrDefault(x => x.FileNames.Contains(name));
        if (fileIcon != null)
        {
            return fileIcon.Name;
        }

        var index = name.LastIndexOf('.');
        if (index >= 0)
        {
            var expandName = name[(index + 1)..];
            var fileIcon2 = fileIcons.FirstOrDefault(x => x.FileExtensions.Contains(expandName));
            if (fileIcon2 != null)
            {
                return fileIcon2.Name;
            }

            var languageIcon = languageIcons.FirstOrDefault(x => x.Ids.Contains(expandName));
            if (languageIcon != null)
            {
                return languageIcon.Icon.Name;
            }
        }

        return IsFolder ? "folder" : "file";
    }
}
