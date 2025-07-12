using BearWordsAPI.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APITest.Helpers;

public static class Timestamp
{
    public static DateTime Generate(int timestamp)
    {
        return new DateTime(1, 1, 1, 0, 0, 0, timestamp);
    }

    public static long GenerateDatetimeLong(int timestamp)
    {
        return new DateTime(1, 1, 1, 0, 0, 0, timestamp).ToLong();
    }

    public static string GenerateDatetimeString(int timestamp)
    {
        return new DateTime(1, 1, 1, 0, 0, 0, timestamp).ToText();
    }
}
