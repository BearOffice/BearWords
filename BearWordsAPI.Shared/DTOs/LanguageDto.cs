using BearWordsAPI.Shared.Data.Models;

namespace BearWordsAPI.Shared.DTOs;

public class LanguageDto
{
    public string LanguageCode { get; set; } = null!;
    public string LanguageName { get; set; } = null!;

    public LanguageDto() { }

    public LanguageDto(Language language)
    {
        LanguageCode = language.LanguageCode;
        LanguageName = language.LanguageName;
    }

    public Language ToEntity()
    {
        return new Language
        {
            LanguageCode = LanguageCode,
            LanguageName = LanguageName
        };
    }
}
