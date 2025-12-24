using System.Collections.Generic;
using Dpz.Core.WebMore.Models;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Shared.Components;

public partial class ArticleItems : ComponentBase
{
    [Parameter]
    public required IEnumerable<ArticleMiniModel> Articles { get; set; }
}