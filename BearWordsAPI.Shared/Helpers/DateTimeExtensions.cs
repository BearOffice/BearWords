namespace BearWordsAPI.Shared.Helpers;

public static class DateTimeExtensions
{
    public static long ToLong(this DateTime dateTime)
    {
        return dateTime.Ticks;
    }

    public static string ToText(this DateTime dateTime)
    {
        return dateTime.Ticks.ToString();
    }

    public static DateTime ToDateTime(this string ticksStr)
    {
        return new DateTime(long.Parse(ticksStr), DateTimeKind.Utc);
    }

    public static DateTime ToDateTime(this long ticksLong)
    {
        return new DateTime(ticksLong, DateTimeKind.Utc);
    }

    public static string ToDateTimeText(this long ticksLong)
    {
        return ticksLong.ToString();
    }

    public static long ToDateTimeLong(this string ticksStr)
    {
        return long.Parse(ticksStr);
    }
}
