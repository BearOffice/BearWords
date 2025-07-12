using BearWordsMaui.Services.Helpers;

namespace BearWordsMaui.Services;

public interface ISyncExecService
{
    public event EventHandler<SyncExecStatus>? Changed;
    public bool IsAcceptingExecRequests { get; }
    public SyncExecStatus Status { get; }
    public Task ExecuteAsync(bool noDelay = false);
    public Task CancelAsync();
    public void AcceptExecRequests();
    public void IgnoreExecRequests();
}
