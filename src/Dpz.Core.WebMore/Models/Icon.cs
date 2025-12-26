using System.Collections.Generic;

namespace Dpz.Core.WebMore.Models;

public class Icon
{
    public required string Name { get; set; }
}

public class LanguageIcon
{
    public required Icon Icon { get; set; }

    public List<string> Ids { get; set; } = [];
}

public class FileIcon
{
    public required string Name { get; set; }

    public List<string> FileNames { get; set; } = [];

    public List<string> FileExtensions { get; set; } = [];
}

public class FolderIcon
{
    public required string Name { get; set; }

    public List<string> FolderNames { get; set; } = [];
}
