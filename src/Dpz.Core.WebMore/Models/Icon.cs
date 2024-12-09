using System.Collections.Generic;

namespace Dpz.Core.WebMore.Models;

public class Icon
{
    public string Name { get; set; }
}

public class LanguageIcon
{
    public Icon Icon { get; set; }

    public List<string> Ids { get; set; }
}

public class FileIcon
{
    public string Name { get; set; }

    public List<string> FileNames { get; set; } = new();

    public List<string> FileExtensions { get; set; } = new();
}

public class FolderIcon
{
    public string Name { get; set; }

    public List<string> FolderNames { get; set; } = new();
}