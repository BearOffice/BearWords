using BearMarkupLanguage;
using BearWordsMaui.Services.Helpers;

namespace BearWordsMaui.Services;

public class ConfigService
{
    private readonly BearML _ml;

    public string UserName
    {
        get => _ml.GetValue<string>("user_name");
        set => _ml.ChangeValue("user_name", value);
    }
    public string ClientId
    {
        get => _ml.GetValue<string>("client_id");
        set => _ml.ChangeValue("client_id", value);
    }
    public string ApiEndpoint
    {
        get => _ml.GetValue<string>("api_endpoint");
        set => _ml.ChangeValue("api_endpoint", value);
    }

    public bool ByPassSslValidation
    {
        get => _ml.GetValue<bool>("bypass_ssl_validation");
        set => _ml.ChangeValue("bypass_ssl_validation", value);
    }

    public SyncAction SyncAction
    {
        get => _ml.GetValue<SyncAction>("sync_action");
        set => _ml.ChangeValue("sync_action", value);
    }
    public AppTheme Theme
    {
        get => _ml.GetValue<AppTheme>("theme");
        set => _ml.ChangeValue("theme", value);
    }
    public string DatabasePath
    {
        get => _ml.GetValue<string>("database");
        set => _ml.ChangeValue("database", value);
    }
    public string LogPath
    {
        get => _ml.GetValue<string>("log");
        set => _ml.ChangeValue("log", value);
    }
    public string SpDataIndicator
    {
        get => _ml.GetValue<string>("special data", "indicator");
    }
    public Dictionary<string, string> SpDataDictionary
    {
        get => _ml.GetValue<Dictionary<string, string>>("special data", "dic");
    }
    public DateTime? LastSync
    {
        get => _ml.GetValue<DateTime?>("temp", "last_sync");
        set => _ml.ChangeValue("temp", "last_sync", value);
    }
    public SyncStatus SyncStatus
    {
        get => _ml.GetValue<SyncStatus>("temp", "sync_status");
        set => _ml.ChangeValue("temp", "sync_status", value);
    }
    public int LastPullItemCount
    {
        get => _ml.GetValue<int>("temp", "last_pull_count");
        set => _ml.ChangeValue("temp", "last_pull_count", value);
    }
    public int LastPushItemCount
    {
        get => _ml.GetValue<int>("temp", "last_push_count");
        set => _ml.ChangeValue("temp", "last_push_count", value);
    }
    public long HideDeletedItemBefore
    {
        get => _ml.GetValue<long>("temp", "hide_deleted_item_before");
        set => _ml.ChangeValue("temp", "hide_deleted_item_before", value);
    }
    public const string SP_TAG_HINT = "tag_hint";
    
    public ConfigService(BearML ml)
    {
        _ml = ml;
        EnsureKeyValues();
    }

    private void EnsureKeyValues()
    {
        EnsureKeyValue("user_name", string.Empty);
        EnsureKeyValue("client_id", string.Empty);
        EnsureKeyValue("api_endpoint", string.Empty);
        EnsureKeyValue("bypass_ssl_validation", true);
        EnsureKeyValue("sync_action", SyncAction.OnStart);
        EnsureKeyValue("theme", AppTheme.Unspecified);

#if WINDOWS || MACCATALYST
        var dbPath = "bear_words.db";
        var logPath = "log.txt";
#else
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "bear_words.db");
        var logPath = Path.Combine(FileSystem.AppDataDirectory, "log.txt");
#endif
        EnsureKeyValue("database", dbPath);
        EnsureKeyValue("log", logPath);

        if (!_ml.GetAllSubBlockNames().Contains("special data"))
            _ml.AddEmptyBlock("special data");
        EnsureKeyValue("special data", "indicator", "@Func");
        var dic = new Dictionary<string, string>
        {
            [SP_TAG_HINT] = "@Tag Hint"
        };
        EnsureKeyValue("special data", "dic", dic);

        if (!_ml.GetAllSubBlockNames().Contains("temp"))
            _ml.AddEmptyBlock("temp");
        EnsureKeyValue("temp", "last_sync", default(DateTime?));
        EnsureKeyValue("temp", "sync_status", SyncStatus.Finished);
        EnsureKeyValue("temp", "last_push_count", 0);
        EnsureKeyValue("temp", "last_pull_count", 0);
        EnsureKeyValue("temp", "hide_deleted_item_before", 0);
        EnsureKeyValue("temp", "login_histories", new LoginHistory[] { });
    }

    private void EnsureKeyValue<T>(string key, T defaultValue)
    {
        if (!_ml.ContainsKey(key))
        {
            _ml.AddKeyValue(key, defaultValue);
        }
    }

    private void EnsureKeyValue<T>(string block, string key, T defaultValue)
    {
        if (!_ml.ContainsKey(block, key))
        {
            _ml.AddKeyValue(block, key, defaultValue);
        }
    }

    public LoginHistory? PeekLoginHistory()
    {
        if (!_ml.TryGetValue("temp", "login_histories", out LoginHistory[] histories))
        {
            return null;
        }

        var history = histories.LastOrDefault();
        if (history is null) return null;

        return history;
    }

    public LoginHistory? PeekLoginHistory(string userName)
    {
        if (!_ml.TryGetValue("temp", "login_histories", out LoginHistory[] histories))
        {
            return null;
        }

        var userHistory = histories.FirstOrDefault(h => h.UserName == userName);
        if (userHistory is null) return null;

        return userHistory;
    }

    public LoginHistory? PopLoginHistory(string userName)
    {
        if (!_ml.TryGetValue("temp", "login_histories", out LoginHistory[] histories))
        {
            return null;
        }

        var userHistory = histories.FirstOrDefault(h => h.UserName == userName);
        if (userHistory is null) return null;

        _ml.ChangeValue("temp", "login_histories", histories.Where(h => h != userHistory).ToArray());
        return userHistory;
    }

    public bool PushLoginHistory(LoginHistory history)
    {
        if (!_ml.TryGetValue("temp", "login_histories", out LoginHistory[] histories))
        {
            return false;
        }

        _ml.ChangeValue("temp", "login_histories", histories
            .Where(h => h.UserName != history.UserName)
            .Append(history)
            .ToArray());

        return true;
    }
}
