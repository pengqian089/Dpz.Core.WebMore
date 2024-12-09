using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Shared
{
    public partial class Tags
    {
        [Inject]private IArticleService ArticleService { get; set; }

        private string[] _tags = Array.Empty<string>();

        private bool _loading = true;

        protected override async Task OnInitializedAsync()
        {
            _tags = await ArticleService.GetTagsAsync();
            _loading = false;
            await base.OnInitializedAsync();
        }
    }
}