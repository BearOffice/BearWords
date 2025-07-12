namespace BearWordsAPI.Shared.Services;

public interface IDateTimeService
{
    public DateTime GetCurrent();

    public long GetCurrentTicksLong()
    {
        return GetCurrent().Ticks;
    }
    
    public string GetCurrentTicksString()
    {
        return GetCurrent().Ticks.ToString();
    }
}
