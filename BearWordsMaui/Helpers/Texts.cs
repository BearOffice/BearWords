using BearWordsAPI.Shared.Helpers;
using BearWordsAPI.Shared.Services;

namespace BearWordsMaui.Helpers;

public static class Texts
{
    public static int LevenshteinDistance(this string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;

        var matrix = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++)
            matrix[i, 0] = i;

        for (int j = 0; j <= target.Length; j++)
            matrix[0, j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                var cost = target[j - 1] == source[i - 1] ? 0 : 1;
                matrix[i, j] = Math.Min(
                    Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost);
            }
        }

        return matrix[source.Length, target.Length];
    }

    public static string GetRelativeTime(this long ticksLong, IDateTimeService dateTimeService)
    {
        return ticksLong.ToDateTime().GetRelativeTime(dateTimeService);
    }

    public static string GetRelativeTime(this DateTime dateTime, IDateTimeService dateTimeService)
    {
        var timeSpan = dateTimeService.GetCurrent() - dateTime;

        if (timeSpan.TotalMinutes < 1)
            return "Just now";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)timeSpan.TotalDays} days ago";
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} months ago";

        return $"{(int)(timeSpan.TotalDays / 365)} years ago";
    }

    public static string TruncateSmart(this string input, int max)
    {
        if (string.IsNullOrEmpty(input) || input.Length <= max)
            return input;

        if (max <= 0)
            return "...";

        int window = 10;
        int start = Math.Max(0, max - window);
        int end = Math.Min(input.Length - 1, max + window);

        int nearestSpace = -1;
        for (int i = max; i >= start; i--)
        {
            if (input[i] == ' ')
            {
                nearestSpace = i;
                break;
            }
        }

        if (nearestSpace == -1)
        {
            for (int i = max + 1; i <= end; i++)
            {
                if (input[i] == ' ')
                {
                    nearestSpace = i;
                    break;
                }
            }
        }

        int cutPoint = nearestSpace != -1 ? nearestSpace : max;

        string truncated = input[..cutPoint].TrimEnd();
        return truncated + " ...";
    }

    public static string Capitalize(this string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        if (input.Length == 1)
            return char.ToUpper(input[0]).ToString();

        return char.ToUpper(input[0]) + input[1..].ToLower();
    }
}
