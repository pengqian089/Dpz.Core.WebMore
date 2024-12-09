using System;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Helper;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages
{
    public partial class Topic
    {
        [Parameter] public string Id { get; set; }
        [Parameter] public int PageIndex { get; set; } = 1;
        [Parameter] public int PageSize { get; set; } = 10;
        [Inject] private IJSRuntime JsRuntime { get; set; }
        [Inject] private NavigationManager Navigation { get; set; }
        [Inject] private ITopicService TopicService { get; set; }

        private TopicModel _topicModel = new();

        private IPagedList<TopicCommentModel> _source =
            new PagedList<TopicCommentModel>(ArraySegment<TopicCommentModel>.Empty, 1, 10);

        private bool _loading = true;

        private static string _topicId = "";
        
        protected override async Task OnInitializedAsync()
        {
            _topicId = "";
            PageIndex = PageIndex == 0 ? 1 : PageIndex;
            PageSize = PageSize == 0 ? 10 : PageSize;
            await base.OnInitializedAsync();
        }
        
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await JsRuntime.InvokeVoidAsync("showLazyImage");
            await JsRuntime.InvokeVoidAsync("Prism.highlightAll");
            await base.OnAfterRenderAsync(firstRender);
        }
        
        protected override async Task OnParametersSetAsync()
        {
            _loading = true;
            if (string.IsNullOrEmpty(_topicId) || _topicId != Id)
            {
                _topicId = Id;
                _topicModel = await TopicService.GetTopicAsync(Id);
            }
            _source = await TopicService.GetTopicCommentPageAsync(Id, PageIndex, PageSize);
            _loading = false;
            await base.OnParametersSetAsync();
        }
        
        private void ToPageAsync(int page)
        {
            PageIndex = page;
            Navigation.NavigateTo(page == 1 ? $"/topic/read/{Id}" : $"/topic/read/{Id}/{page}");
        }
    }
}