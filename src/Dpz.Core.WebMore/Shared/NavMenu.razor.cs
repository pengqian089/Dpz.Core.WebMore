using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Shared;

public partial class NavMenu(NavigationManager navigation, IJSRuntime jsRuntime)
{
    private string _currentUrl = "";
    private bool _isMobileMenuOpen;
    private bool _isSubMenuExpanded;

    protected override void OnInitialized()
    {
        // Get initial URL (relative)
        _currentUrl = navigation.ToBaseRelativePath(navigation.Uri);
        // Remove fragment if any (optional)
        if (_currentUrl.Contains('#'))
        {
            _currentUrl = _currentUrl.Split('#')[0];
        }

        // Auto-expand if we are in a submenu page
        if (!string.IsNullOrEmpty(MoreActive()))
        {
            _isSubMenuExpanded = true;
        }

        navigation.LocationChanged += OnLocationChanged;
    }

    private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        _currentUrl = navigation.ToBaseRelativePath(e.Location);
        if (_currentUrl.Contains('#'))
        {
            _currentUrl = _currentUrl.Split('#')[0];
        }

        // Auto-expand if we are in a submenu page
        if (!string.IsNullOrEmpty(MoreActive()))
        {
            _isSubMenuExpanded = true;
        }

        if (_isMobileMenuOpen)
        {
            _ = InvokeAsync(async () =>
            {
                await CloseMobileMenu();
                StateHasChanged();
            });
        }
        else
        {
            StateHasChanged();
        }
    }

    public void Dispose()
    {
        navigation.LocationChanged -= OnLocationChanged;
    }

    private string IsActive(string href, bool matchAll = false)
    {
        // Remove query string for comparison
        var path = _currentUrl.Split('?')[0];

        if (matchAll)
        {
            return string.IsNullOrEmpty(path) ? "nav-menu__item--active" : "";
        }

        if (string.IsNullOrEmpty(path))
        {
            return "";
        }

        return path.StartsWith(href, StringComparison.OrdinalIgnoreCase)
            ? "nav-menu__item--active"
            : "";
    }

    private string MoreActive()
    {
        var path = _currentUrl.Split('?')[0];
        var moreLinks = new[] { "albums", "code", "timeline", "friends" };
        foreach (var link in moreLinks)
        {
            if (path.StartsWith(link, StringComparison.OrdinalIgnoreCase))
            {
                return "nav-menu__item--active";
            }
        }
        return "";
    }

    private async Task ToggleMobileMenu()
    {
        _isMobileMenuOpen = !_isMobileMenuOpen;
        await UpdateBodyOverflow();
    }

    private async Task CloseMobileMenu()
    {
        if (_isMobileMenuOpen)
        {
            _isMobileMenuOpen = false;
            // _isSubMenuExpanded = false; // Don't collapse submenu automatically
            await UpdateBodyOverflow();
            StateHasChanged();
        }
    }

    private void ToggleSubMenu()
    {
        _isSubMenuExpanded = !_isSubMenuExpanded;
    }

    private async Task UpdateBodyOverflow()
    {
        try
        {
            await jsRuntime.InvokeVoidAsync(
                "eval",
                $"document.body.style.overflow = '{(_isMobileMenuOpen ? "hidden" : "")}'"
            );
        }
        catch
        {
            // Ignore JS errors (e.g. if pre-rendering)
        }
    }
}
