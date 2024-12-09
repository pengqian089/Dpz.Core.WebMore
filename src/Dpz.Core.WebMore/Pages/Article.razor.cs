using System;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Dpz.Core.WebMore.Shared;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.JSInterop;
using MudBlazor;

namespace Dpz.Core.WebMore.Pages
{
    public partial class Article
    {
        [Parameter] public string Id { get; set; }
        [Inject] private IArticleService ArticleService { get; set; }
        [Inject] private IJSRuntime JsRuntime { get; set; }
        [Inject] private IDialogService DialogService { get; set; }
        [Inject] private ISnackbar Snackbar { get; set; }

        private ArticleModel _article = new();

        private bool _loading = true;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JsRuntime.InvokeVoidAsync("Prism.highlightAll");
            await base.OnAfterRenderAsync(firstRender);
        }

        protected override async Task OnParametersSetAsync()
        {
            _loading = true;
            _article = await ArticleService.GetArticleAsync(Id);
            _loading = false;
            await base.OnParametersSetAsync();
        }

        private void ShowPay()
        {
            DialogService.Show<WeChatPay>("",new DialogOptions{CloseButton = true});
        }

        private void Like()
        {
            Snackbar.Configuration.PositionClass = Defaults.Classes.Position.TopCenter;
            Snackbar.Add("点赞接口还未实现", Severity.Normal);
        }
    }
}