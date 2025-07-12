using BearMarkupLanguage;
using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.Shared.Helpers;
using BearWordsAPI.Shared.Services;
using BearWordsMaui.Services;
using BearWordsMaui.Services.DbServices;
using BearWordsMaui.Services.Helpers;
using Microsoft.Extensions.Logging;
using System.Net.Security;

namespace BearWordsMaui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>()
               .ConfigureFonts(fonts =>
               {
                   fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
               });

        ConfigureServices(builder);

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        AppDomain.CurrentDomain.UnhandledException += (obj, e) => OnUnhandledException(obj, e, app.Services);
        TaskScheduler.UnobservedTaskException += (obj, e) => OnUnobservedTaskException(obj, e, app.Services);

        var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
        using (var scope = scopeFactory.CreateScope())
        {
            // Ensure client id generated.
            var config = scope.ServiceProvider.GetRequiredService<ConfigService>();

            if (string.IsNullOrWhiteSpace(config.ClientId))
            {
                var uuid = scope.ServiceProvider.GetRequiredService<IUUIDGenerator>();
                config.ClientId = uuid.Generate()[..13];
            }

            // Ensure db created.
            var db = scope.ServiceProvider.GetRequiredService<IDbContextService>();
            var context = db.GetDbContext();
            context.Database.EnsureCreated();
        }

        return app;
    }

    private static void ConfigureServices(MauiAppBuilder builder)
    {
#if WINDOWS
        var configPath = Path.Combine(Environment.CurrentDirectory, "configs.txt");
#else
        var configPath = Path.Combine(FileSystem.AppDataDirectory, "configs.txt");
#endif

        builder.Services.AddSingleton(_ => new ConfigService(new BearML(configPath)));
        builder.Services.AddSingleton<ILogService, LogService>();

#if ANDROID
        builder.Services.AddSingleton<IWakeLockService, Platforms.Android.WakeLockService>();
#else
        builder.Services.AddSingleton<IWakeLockService, Services.WakeLockService>();
#endif

        builder.Services.AddScoped<ISyncExecutor, SyncService>();
        builder.Services.AddScoped<ISyncUtils, SyncService>();
        builder.Services.AddScoped<ISyncExecService, SyncExecService>();
        builder.Services.AddScoped<IInitService, InitService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IDateTimeService, DateTimeService>();
        builder.Services.AddScoped<IUUIDGenerator, UUIDGenerator>();
        builder.Services.AddScoped<ITriggerSourceFactory, TriggerSourceFactory>();
        builder.Services.AddScoped<ITempStorageService, TempStorageService>();
        builder.Services.AddScoped<JwtAuthHandler>();
        builder.Services.AddScoped<ScrollManager>();

        builder.Services.AddScoped<SearchService>();
        builder.Services.AddScoped<PhraseService>();
        builder.Services.AddScoped<WordService>();
        builder.Services.AddScoped<LanguageService>();
        builder.Services.AddScoped<BookmarkService>();
        builder.Services.AddScoped<TagService>();
        builder.Services.AddScoped<TagHintService>();
        builder.Services.AddScoped<SpecialDataService>();
        builder.Services.AddScoped<ImportService>();
        builder.Services.AddScoped<ExportService>();
        builder.Services.AddScoped<RestoreService>();
        builder.Services.AddScoped<ConflictService>();
        builder.Services.AddScoped<BasicInfoService>();

        builder.Services.AddScoped(_ => BuildCascadeConfig());

        builder.Services.AddHttpClient("AuthClient")
            .ConfigurePrimaryHttpMessageHandler(CreateHttpHandler);

        builder.Services.AddHttpClient("SyncClient")
            .AddHttpMessageHandler<JwtAuthHandler>()
            .ConfigurePrimaryHttpMessageHandler(CreateHttpHandler);

        builder.Services.AddScoped<IApiHttpClientFactory, ApiHttpClientFactory>();
        builder.Services.AddScoped<IDbContextService, DbContextService>();

        builder.Services.AddMauiBlazorWebView();
    }

    private static SocketsHttpHandler CreateHttpHandler(IServiceProvider serviceProvider)
    {
        var handler = new SocketsHttpHandler
        {
            ConnectTimeout = TimeSpan.FromSeconds(5),
        };

        handler.SslOptions.RemoteCertificateValidationCallback = (sender, certificate, chain, errors) =>
        {
            using var scope = serviceProvider.CreateScope();
            var config = scope.ServiceProvider.GetRequiredService<ConfigService>();

            if (config.ByPassSslValidation)
                return true;

            return errors == SslPolicyErrors.None;
        };

        return handler;
    }

    private static CascadeConfiguration BuildCascadeConfig()
    {
        var builder = new CascadeRuleBuilder();
        return builder
            .OnRestore<Bookmark, BookmarkTag>((b, context) =>
                context.Entry(b)
                       .Collection(b => b.BookmarkTags)
                       .Query()
                       .WhereDeleted()
                       .Where(bt => bt.ModifiedAt == b.ModifiedAt))
            .OnRestore<BookmarkTag, Tag>((bt, context) =>
            {
                var tag = context.Entry(bt)
                    .Reference(bt => bt.Tag)
                    .CurrentValue;
                if (tag is null || !tag.DeleteFlag) return Enumerable.Empty<Tag>().AsQueryable();
                else return new[] { tag }.AsQueryable();
            })
            .OnRestore<Phrase, PhraseTag>((p, context) =>
                context.Entry(p)
                       .Collection(p => p.PhraseTags)
                       .Query()
                       .WhereDeleted()
                       .Where(pt => pt.ModifiedAt == p.ModifiedAt))
            .OnRestore<PhraseTag, Tag>((pt, context) =>
            {
                var tag = context.Entry(pt)
                    .Reference(pt => pt.Tag)
                    .CurrentValue;
                if (tag is null || !tag.DeleteFlag) return Enumerable.Empty<Tag>().AsQueryable();
                else return new[] { tag }.AsQueryable();
            })
            .OnRestore<TagCategory, Tag>((tc, context) =>
                context.Entry(tc)
                       .Collection(tc => tc.Tags)
                       .Query()
                       .WhereDeleted()
                       .Where(t => t.ModifiedAt == tc.ModifiedAt)
            )
            .Build();
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e,
        IServiceProvider serviceProvider)
    {
        try
        {
            var logService = serviceProvider?.GetService<ILogService>();
            var errMsg = e.ExceptionObject?.ToString() ?? "Unknown error";
            logService?.Error($"Unhandled Exception: {errMsg}");

            System.Diagnostics.Debug.WriteLine($"Unhandled Exception: {errMsg}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to log exception: {ex}");
        }
    }

    private static void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e,
        IServiceProvider serviceProvider)
    {
        try
        {
            var logService = serviceProvider?.GetService<ILogService>();
            var errMsg = e.Exception?.ToString() ?? "Unknown task exception";
            logService?.Error($"Unobserved Task Exception: {errMsg}");

            System.Diagnostics.Debug.WriteLine($"Unobserved Task Exception: {errMsg}");
            e.SetObserved();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to log task exception: {ex}");
        }
    }
}
