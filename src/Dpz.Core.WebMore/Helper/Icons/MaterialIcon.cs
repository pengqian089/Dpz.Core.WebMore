using System.Collections.Generic;
using System.Text.Json;
using Dpz.Core.WebMore.Models;

namespace Dpz.Core.WebMore.Helper.Icons;

public static partial class MaterialIcon
{
    private static List<FileIcon> _fileIcons = [];

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public static List<FileIcon> GetFileIcons()
    {
        if (_fileIcons.Count == 0)
        {
            _fileIcons = JsonSerializer.Deserialize<List<FileIcon>>(FileIconsJson, Options) ?? [];
        }

        return [.. _fileIcons];
    }

    private static List<FolderIcon> _folderIcons = [];

    public static List<FolderIcon> GetFolderIcons()
    {
        if (_folderIcons.Count == 0)
        {
            _folderIcons =
                JsonSerializer.Deserialize<List<FolderIcon>>(FolderIconsJson, Options) ?? [];
        }
        return [.. _folderIcons];
    }

    private static List<LanguageIcon> _languageIcons = [];

    public static List<LanguageIcon> GetLanguageIcons()
    {
        if (_languageIcons.Count == 0)
        {
            _languageIcons =
                JsonSerializer.Deserialize<List<LanguageIcon>>(LanguageIconsJson, Options) ?? [];
        }
        return [.. _languageIcons];
    }
}
