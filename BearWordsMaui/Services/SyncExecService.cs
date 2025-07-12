using BearWordsMaui.Services.Helpers;
using TriggerLib;

namespace BearWordsMaui.Services;

public class SyncExecService : ISyncExecService
{
    public event EventHandler<SyncExecStatus>? Changed;
    public SyncExecStatus Status { get; private set; }
    public bool IsAcceptingExecRequests { get; private set; }

    private readonly ISyncExecutor _syncExecutor;
    private readonly ITriggerSource _triggerSource;
    private readonly ILogService _logService;
    private readonly IDbContextService _contextService;
    private readonly ConfigService _config;
    private CancellationTokenSource? _tokenSource;

    public SyncExecService(ISyncExecutor syncExecutor, ITriggerSourceFactory triggerSourceFactory,
        ILogService logService, IDbContextService contextService, ConfigService config)
    {
        _logService = logService;
        _syncExecutor = syncExecutor;
        _triggerSource = triggerSourceFactory.CreateTriggerSource(3000, async () =>
            {
                await _syncExecutor.SyncAsync();

                Status = SyncExecStatus.Idling;
                Changed?.Invoke(this, Status);
            });
        _triggerSource.OnException += TriggerSource_OnException;

        _config = config;
        _contextService = contextService;
        _contextService.BeforeSaveChanges += ContextService_BeforeSaveChanges;

        Status = SyncExecStatus.Idling;
        IsAcceptingExecRequests = true;
    }

    private void ContextService_BeforeSaveChanges(object? sender, EventArgs e)
    {
        if (!IsAcceptingExecRequests) return;
        if (Status == SyncExecStatus.Running) return;

        if (_config.SyncAction == SyncAction.OnAction)
        {
            ExecuteAsync().Wait();
        }
    }

    private void TriggerSource_OnException(Exception obj)
    {
        Status = SyncExecStatus.Failed;
        Changed?.Invoke(this, Status);

        _logService.Error(obj.ToString());
    }

    public async Task ExecuteAsync(bool noDelay = false)
    {
        if (!IsAcceptingExecRequests) return;
        if (Status == SyncExecStatus.Running) return;

        if (noDelay)
        {
            _tokenSource = new CancellationTokenSource();
            await Task.Run(_syncExecutor.SyncAsync, _tokenSource.Token);

            _tokenSource = null;
            return;
        }

        Status = SyncExecStatus.Running;
        Changed?.Invoke(this, Status);

        _triggerSource.Start();

        return;
    }

    public Task CancelAsync()
    {
        if (Status != SyncExecStatus.Running) return Task.CompletedTask;

        _tokenSource?.Cancel();  // For no delay task.
        _triggerSource.Cancel();

        Status = SyncExecStatus.Idling;
        Changed?.Invoke(this, Status);

        return Task.CompletedTask;
    }

    public void AcceptExecRequests()
    {
        IsAcceptingExecRequests = true;
    }

    public void IgnoreExecRequests()
    {
        IsAcceptingExecRequests = false;
    }
}
