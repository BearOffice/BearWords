namespace BearWordsMaui.Services;

public interface ISyncUtils
{
    public Task<DateTime> GetServerTimeAsync();
    public string[] GetToPushItemIds();
    public int GetToPushItemCount();
    public int GetLastPullItemCount();
    public int GetLastPushItemCount();
    public DateTime? GetLastSyncDateTime();
    public Task<bool> RegisterClientAsync(string clientId);
}