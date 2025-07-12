using BearWordsMaui.Services;
using Microsoft.Maui.Controls.PlatformConfiguration.AndroidSpecific;
using Application = Microsoft.Maui.Controls.Application;

namespace BearWordsMaui;

public partial class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ConfigService _config;

    public App(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        _config = _serviceProvider.GetRequiredService<ConfigService>();

        InitializeComponent();

        // Enable android soft input mode
        Current!.On<Microsoft.Maui.Controls.PlatformConfiguration.Android>()
            .UseWindowSoftInputModeAdjust(WindowSoftInputModeAdjust.Resize);

        Current!.UserAppTheme = _config.Theme;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        if (!string.IsNullOrEmpty(_config.UserName) && !string.IsNullOrEmpty(_config.ApiEndpoint))
        {
            return new Window(new MainPage(_config)) { Title = "BearWordsMaui" };
        }
        else
        {
            return new Window(new MainPage("/login", _config)) { Title = "BearWordsMaui" };
        }
    }
}
