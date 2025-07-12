namespace BearWordsMaui.Services;

public interface IAuthService
{
    public Task<string?> GetValidTokenAsync();
    public Task<string?> LoginAsync();
    public Task RefreshTokenAsync();
}
