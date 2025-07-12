using BearWordsMaui.Services;

public class ApiHttpClientFactory : IApiHttpClientFactory
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ConfigService _configService;

    public ApiHttpClientFactory(IHttpClientFactory httpClientFactory, ConfigService configService)
    {
        _httpClientFactory = httpClientFactory;
        _configService = configService;
    }

    public HttpClient CreateAuthClient()
    {
        var client = _httpClientFactory.CreateClient("AuthClient");
        if (client.BaseAddress == null && !string.IsNullOrEmpty(_configService.ApiEndpoint))
        {
            client.BaseAddress = new Uri(_configService.ApiEndpoint);
            client.Timeout = TimeSpan.FromSeconds(10);
        }
        return client;
    }

    public HttpClient CreateSyncClient()
    {
        var client = _httpClientFactory.CreateClient("SyncClient");
        if (client.BaseAddress == null && !string.IsNullOrEmpty(_configService.ApiEndpoint))
        {
            client.BaseAddress = new Uri(_configService.ApiEndpoint);
        }
        return client;
    }
}