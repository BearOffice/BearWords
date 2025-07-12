namespace BearWordsAPI.Services;

using BearMarkupLanguage;
using System.Security.Cryptography;

public class ConfigService
{
    private readonly BearML _ml;

    public string[] UserNames { get => _ml.GetValue<string[]>("users"); }
    public string IssuerKey { get => _ml.GetValue<string>("issuer_key"); }
    public string Database { get => _ml.GetValue<string>("database"); }

    public ConfigService(BearML ml)
    {
        _ml = ml;
        EnsureKeyValues();
    }

    private void EnsureKeyValues()
    {
        EnsureKeyValue("users", new string[] { "admin" });
        EnsureKeyValue("issuer_key", GenerateHex());
        EnsureKeyValue("database", "data/bear_words.db");
    }

    private void EnsureKeyValue<T>(string key, T defaultValue)
    {
        if (!_ml.ContainsKey(key))
        {
            _ml.AddKeyValue(key, defaultValue);
        }
    }

    private static string GenerateHex()
    {
        byte[] bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToHexString(bytes);
    }
}
