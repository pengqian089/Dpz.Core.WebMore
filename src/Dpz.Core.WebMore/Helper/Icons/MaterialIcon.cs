using System.Collections.Generic;
using System.Text.Json;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Helper.Icons;

public static partial class MaterialIcon
{
    private static List<FileIcon> _fileIcons;

    private static readonly JsonSerializerOptions Options = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    };

    public static List<FileIcon> GetFileIcons()
    {
        return _fileIcons ??= JsonSerializer.Deserialize<List<FileIcon>>(FileIconsJson,Options);
    }

    private static List<FolderIcon> _folderIcons;

    public static List<FolderIcon> GetFolderIcons()
    {
        return _folderIcons ??= JsonSerializer.Deserialize<List<FolderIcon>>(FolderIconsJson,Options);
    }

    private static List<LanguageIcon> _languageIcons;

    public static List<LanguageIcon> GetLanguageIcons()
    {
        return _languageIcons ??= JsonSerializer.Deserialize<List<LanguageIcon>>(LanguageIconsJson,Options);
    }
}