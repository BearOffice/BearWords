using BearWordsMaui.Services;
using Microsoft.AspNetCore.Components.WebView.Maui;

namespace BearWordsMaui;

public partial class MainPage : ContentPage
{
    private readonly ConfigService _config;

    public MainPage(ConfigService config)
    {
        InitializeComponent();

        _config = config;
        Loaded += ContentPage_Loaded;
        AddMapper();
    }

    public MainPage(string route, ConfigService config)
    {
        InitializeComponent();

        _config = config;
        Loaded += ContentPage_Loaded;
        blazorWebView.StartPath = route;
        AddMapper();
    }

    private void AddMapper()
    {
        BlazorWebViewHandler.BlazorWebViewMapper.AppendToMapping("CustomBlazorWebViewMapper", (handler, view) =>
        {
#if WINDOWS
            var theme = _config.Theme;
            if (theme == AppTheme.Dark || 
                (theme == AppTheme.Unspecified && AppInfo.Current.RequestedTheme == AppTheme.Dark))
            {
                handler.PlatformView.DefaultBackgroundColor = Microsoft.UI.Colors.Black;
            }
#endif

#if IOS || MACCATALYST
            handler.PlatformView.BackgroundColor = UIKit.UIColor.Clear;
            handler.PlatformView.Opaque = false;
#endif

#if ANDROID
            handler.PlatformView.SetBackgroundColor(Android.Graphics.Color.Transparent);
#endif
        });
    }

    private async void ContentPage_Loaded(object? sender, EventArgs e)
    {
#if WINDOWS && RELEASE
        var webView2 = (blazorWebView.Handler!.PlatformView as Microsoft.UI.Xaml.Controls.WebView2);
        await webView2!.EnsureCoreWebView2Async();

        var settings = webView2.CoreWebView2.Settings;
        settings.AreBrowserAcceleratorKeysEnabled = false;
        settings.IsZoomControlEnabled = false;
#endif
    }
}
