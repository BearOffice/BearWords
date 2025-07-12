namespace BearWordsMaui.Services.Helpers;

public class TokenInfo
{
    public required string Token { get; init; }
    public required DateTime ExpiresAt { get; init; }
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt.AddMinutes(-5);
}