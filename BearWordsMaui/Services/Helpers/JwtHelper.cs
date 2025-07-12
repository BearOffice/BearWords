using System.IdentityModel.Tokens.Jwt;

namespace BearWordsMaui.Services.Helpers;

public static class JwtHelper
{
    public static DateTime? GetExpirationTime(string jwtToken)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwtToken);

            var expClaim = token.Claims.FirstOrDefault(c => c.Type == JwtRegisteredClaimNames.Exp);

            if (expClaim is not null && long.TryParse(expClaim.Value, out long exp))
            {
                return DateTimeOffset.FromUnixTimeSeconds(exp).DateTime;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public static bool IsTokenExpired(string jwtToken)
    {
        var expirationTime = GetExpirationTime(jwtToken);

        if (expirationTime is not null)
        {
            return DateTime.UtcNow > expirationTime.Value;
        }

        return true;
    }

    public static TimeSpan GetTimeUntilExpiration(string jwtToken)
    {
        var expirationTime = GetExpirationTime(jwtToken);

        if (expirationTime is not null)
        {
            var remaining = expirationTime.Value - DateTime.UtcNow;
            return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
        }

        return TimeSpan.Zero;
    }
}
