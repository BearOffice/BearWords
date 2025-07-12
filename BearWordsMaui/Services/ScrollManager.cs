using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.JSInterop;

namespace BearWordsMaui.Services;

public class ScrollManager : IDisposable
{
    private readonly NavigationManager _navManager;
    private readonly IJSRuntime _js;
    private readonly IDisposable? _locationChanging;

    public ScrollManager(NavigationManager navManager, IJSRuntime js)
    {
        _navManager = navManager;
        _js = js;

        _locationChanging = _navManager.RegisterLocationChangingHandler(OnLocationChanging);
        _navManager.LocationChanged += OnLocationChanged;
    }

    private async ValueTask OnLocationChanging(LocationChangingContext context)
    {
        await _js.InvokeVoidAsync("saveScrollBeforeBlazorNav");
    }

    private async void OnLocationChanged(object? sender, LocationChangedEventArgs e)
    {
        await _js.InvokeVoidAsync("restoreScrollFromSession");
    }

    public async Task ClearScrollPositions()
    {
        await _js.InvokeVoidAsync("clearAllScrollPositions");
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        _locationChanging?.Dispose();
        _navManager.LocationChanged -= OnLocationChanged;
    }
}