using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;

namespace Dpz.Core.WebMore.Pages;

public partial class Friends(ICommunityService communityService) : ComponentBase
{
    private List<FriendModel> _friends = [];
    private bool _isLoading = true;

    protected override async Task OnInitializedAsync()
    {
        try
        {
            if (_friends.Count == 0)
            {
                _friends = await communityService.GetFriendsAsync();
            }
        }
        finally
        {
            _isLoading = false;
        }

        await base.OnInitializedAsync();
    }
}
