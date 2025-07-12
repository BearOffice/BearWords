namespace BearWordsMaui.Services;

public class WakeLockService : IWakeLockService
{
    public void AcquireWakeLock() { }
    public void ReleaseWakeLock() { }
    public bool IsWakeLockHeld() => false;
}