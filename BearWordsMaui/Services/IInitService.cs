namespace BearWordsMaui.Services;

public interface IInitService
{
    public Task SetState(string userName, string apiEndpoint);
    public Task ClearState();
    public Task RegisterClient();
    public Task PullData();
}