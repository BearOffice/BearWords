using TriggerLib;

namespace BearWordsMaui.Services;

public interface ITriggerSourceFactory
{
    public ITriggerSource CreateTriggerSource(TimeSpan delay, Func<Task> action);
    public ITriggerSource CreateTriggerSource(TimeSpan delay, Action action);

    public ITriggerSource CreateTriggerSource(long delay, Func<Task> action)
    {
        return CreateTriggerSource(TimeSpan.FromMilliseconds((double)delay), action);
    }

    public ITriggerSource CreateTriggerSource(long delay, Action action)
    {
        return CreateTriggerSource(TimeSpan.FromMilliseconds((double)delay), action);
    }
}
