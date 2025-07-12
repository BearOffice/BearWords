#if ANDROID
using Android.OS;
using Android.Content;
using BearWordsMaui.Services;
using static Android.OS.PowerManager;

namespace BearWordsMaui.Platforms.Android;

public class WakeLockService : IWakeLockService
{
    private WakeLock? _wakeLock;

    public void AcquireWakeLock()
    {
        if (_wakeLock?.IsHeld == true)
            return;

        var context = Platform.CurrentActivity?.ApplicationContext;
        var powerManager = (PowerManager?)context?.GetSystemService(Context.PowerService);
        if (powerManager != null)
        {
            _wakeLock = powerManager.NewWakeLock(WakeLockFlags.ScreenDim, "MyApp:WakeLock");
            _wakeLock?.Acquire();
        }
        else
        {
            throw new InvalidOperationException();
        }
    }

    public void ReleaseWakeLock()
    {
        if (_wakeLock?.IsHeld == true)
        {
            _wakeLock.Release();
        }
    }

    public bool IsWakeLockHeld()
    {
        return _wakeLock?.IsHeld == true;
    }
}
#endif