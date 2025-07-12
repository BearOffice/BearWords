using TriggerLib;

namespace BearWordsMaui.Services;

public class TriggerSourceFactory: ITriggerSourceFactory
{
    public ITriggerSource CreateTriggerSource(TimeSpan delay, Func<Task> action) 
        => new TriggerSource(delay, action);

    public ITriggerSource CreateTriggerSource(TimeSpan delay, Action action) 
        => new TriggerSource(delay, action);
}
