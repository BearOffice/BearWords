using BearWordsAPI.Shared.Data;
using BearWordsAPI.Shared.Helpers;
using BearWordsAPI.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace APITest.Helpers;

public class MockDateTimeService : IDateTimeService
{
    public int Timestamp { get; private set; }

    public DateTime GetCurrent()
    {
        var t = Timestamp;
        Timestamp++;

        return Helpers.Timestamp.Generate(t);
    }

    public void Next(int timestamp)
    {
        Timestamp = timestamp;
    }

    public long GetCurrentTicksLong()
    {
        return GetCurrent().ToLong();
    }
}

