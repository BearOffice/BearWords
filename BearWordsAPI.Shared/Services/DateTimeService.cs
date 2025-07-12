namespace BearWordsAPI.Shared.Services;

public class DateTimeService : IDateTimeService
{
    public DateTime GetCurrent()
    {
        return DateTime.UtcNow;
    }
}
