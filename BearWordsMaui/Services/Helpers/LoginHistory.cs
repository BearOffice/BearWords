using SerializableAttribute = BearMarkupLanguage.Serialization.SerializableAttribute;

namespace BearWordsMaui.Services.Helpers;

[Serializable]
public class LoginHistory
{
    public string? UserName { get; set; }
    public string? ApiEndpoint { get; set; }
    public string? ClientId { get; set; }
}