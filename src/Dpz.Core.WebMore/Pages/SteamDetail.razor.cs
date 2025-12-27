using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dpz.Core.WebMore.Models;
using Dpz.Core.WebMore.Service;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Dpz.Core.WebMore.Pages;

public partial class SteamDetail(ISteamService steamService, IJSRuntime jsRuntime): ComponentBase
{
    [Parameter]
    [EditorRequired]
    public required int Id { get; set; }

    private SteamModel? _model;
    private bool _isLoading = true;
    private bool _animate;
    private readonly Dictionary<string, bool> _collapsedGroups = new();

    private bool IsCyberpunk2077 => _model?.Name?.Contains("Cyberpunk 2077", StringComparison.OrdinalIgnoreCase) == true;

    protected override async Task OnInitializedAsync()
    {
        if (_model == null)
        {
            _isLoading = true;
            _model = await steamService.GetLibraryAsync(Id);

            if (_model != null)
            {
                var keys = new[]
                {
                    $"unlocked-{_model.Id}",
                    $"locked-{_model.Id}",
                    $"hidden-{_model.Id}",
                };
                foreach (var key in keys)
                {
                    try
                    {
                        var storedValue = await jsRuntime.InvokeAsync<string>(
                            "localStorage.getItem",
                            $"steam-group-{key}"
                        );
                        if (
                            !string.IsNullOrEmpty(storedValue)
                            && bool.TryParse(storedValue, out var isCollapsed)
                        )
                        {
                            _collapsedGroups[key] = isCollapsed;
                        }
                    }
                    catch
                    {
                        // ignore
                    }
                }
            }

            _isLoading = false;
        }

        await base.OnInitializedAsync();
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!_isLoading && _model != null && !_animate)
        {
            // 动画
            await Task.Delay(100);
            _animate = true;
            StateHasChanged();
        }
        await base.OnAfterRenderAsync(firstRender);
    }

    private bool IsCollapsed(string key, bool defaultValue)
    {
        return _collapsedGroups.GetValueOrDefault(key, defaultValue);
    }

    private async Task ToggleGroup(string key, bool defaultValue)
    {
        var current = IsCollapsed(key, defaultValue);
        var newValue = !current;
        _collapsedGroups[key] = newValue;

        await jsRuntime.InvokeVoidAsync(
            "localStorage.setItem",
            $"steam-group-{key}",
            newValue.ToString()
        );
    }
}
