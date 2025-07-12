using BearWordsAPI.Shared.DTOs;
using BearWordsMaui.Services.Helpers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BearWordsMaui.Services;

public record UserLogin(string UserName);
public record Jwt(string Token);

public class AuthService : IAuthService
{
    private readonly IApiHttpClientFactory _httpClientsFactory;
    private readonly SemaphoreSlim _tokenSemaphore = new(1, 1);

    private TokenInfo? _currentToken;
    private readonly ConfigService _config;

    public AuthService(IApiHttpClientFactory httpClientFactory, ConfigService config)
    {
        _httpClientsFactory = httpClientFactory;
        _config = config;
    }

    public async Task<string?> GetValidTokenAsync()
    {
        await _tokenSemaphore.WaitAsync();

        try
        {
            if (_currentToken is null || _currentToken.IsExpired)
            {
                await RefreshTokenAsync();
            }

            return _currentToken?.Token;
        }
        finally
        {
            _tokenSemaphore.Release();
        }
    }

    public async Task<string?> LoginAsync()
    {
        var loginRequest = new UserLogin(_config.UserName);

        var json = JsonSerializer.Serialize(loginRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClientsFactory.CreateAuthClient().PostAsync("login", content);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Authentication failed: {response.StatusCode}.");
        }
        //var responseContent = await response.Content.ReadAsStringAsync();
        //var jwt = JsonSerializer.Deserialize<Jwt>(responseContent);

        var jwt = await response.Content.ReadFromJsonAsync<Jwt>();

        return jwt?.Token;
    }

    public async Task RefreshTokenAsync()
    {
        try
        {
            var tokenResponse = await LoginAsync();

            _currentToken = new TokenInfo
            {
                Token = tokenResponse!,
                ExpiresAt = JwtHelper.GetExpirationTime(tokenResponse!)!.Value
            };
        }
        catch
        {
            _currentToken = null;
            throw;
        }
    }
}