namespace BearWordsMaui.Services;

public interface IWakeLockService
{
    public void AcquireWakeLock();
    public void ReleaseWakeLock();
    public bool IsWakeLockHeld();
}