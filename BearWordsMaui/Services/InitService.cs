
using BearWordsAPI.Shared.Services;
using BearWordsMaui.Services.Helpers;

namespace BearWordsMaui.Services;

public class InitService : IInitService
{
    private readonly ConfigService _config;
    private readonly IDbContextService _dbContextService;
    private readonly ISyncUtils _syncUtils;
    private readonly ISyncExecService _syncService;
    private readonly ILogService _logService;
    private BearWordsContext Context => _dbContextService.GetDbContext();

    public InitService(IDbContextService dbContextService, ConfigService config, 
        ISyncUtils syncUtils, ISyncExecService syncService, ILogService logService)
    {
        _dbContextService = dbContextService;
        _config = config;
        _syncUtils = syncUtils;
        _syncService = syncService;
        _logService = logService;
    }

    public Task ClearState()
    {
        _config.PushLoginHistory(new Helpers.LoginHistory
        {
            UserName = _config.UserName,
            ApiEndpoint = _config.ApiEndpoint,
            ClientId = _config.ClientId,
        });

        _config.UserName = string.Empty;
        _config.ApiEndpoint = string.Empty;

        _logService.ClearLogs();

        _dbContextService.CreateNewDbContext();

        return Task.CompletedTask;
    }

    public async Task SetState(string userName, string apiEndpoint)
    {
        if (!string.IsNullOrEmpty(_config.UserName))
            throw new Exception("The state is not cleared yet. Call `ClearState()` first.");

        var history = _config.PopLoginHistory(userName);

        _config.UserName = userName;
        _config.ApiEndpoint = apiEndpoint;

        _dbContextService.CreateNewDbContext();

        // If user switch the api endpoint, clear the user's local data.
        if (history is not null && history.ApiEndpoint != apiEndpoint)
        {
            await Context.ClearUserDataAsync(userName);
        }

        // Set user and client id if not exists
        await Context.EnsureUserAsync(userName);
        await Context.EnsureClientIdAsync(userName, _config.ClientId);
    }

    public async Task RegisterClient()
    {
        await _syncUtils.RegisterClientAsync(_config.ClientId);
    }

    public async Task PullData()
    {
        await _syncService.ExecuteAsync(noDelay: true);
    }
}