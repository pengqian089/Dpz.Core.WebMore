using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Dpz.Core.WebMore.Models;

public class PushMessageModel
{
    public string Markdown { get; set; } = "";
}

public class SubscribeModel
{
    public string Message { get; set; } = "";
    public int Type { get; set; }
    public List<double> ProgressValues { get; set; } = [];
}

