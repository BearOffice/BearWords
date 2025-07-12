using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BearWordsMaui.Components;

public abstract class BasePageComponent : ComponentBase
{
    [Inject] protected NavigationManager Navigation { get; set; } = null!;
    [Inject] protected IJSRuntime JSRuntime { get; set; } = null!;

    protected async Task NavigateToPrevious()
    {
        await JSRuntime.InvokeVoidAsync("goBack");  // function def in wwwroot/js/app.js
    }

    protected void NavigateToHome()
    {
        Navigation.NavigateTo("/");
    }
}