namespace BearWordsMaui.Services;

public interface IApiHttpClientFactory
{
    public HttpClient CreateAuthClient();
    public HttpClient CreateSyncClient();
}
