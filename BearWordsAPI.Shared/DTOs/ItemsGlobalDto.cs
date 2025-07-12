namespace BearWordsAPI.Shared.DTOs;

public class ItemsGlobalDto
{
    public LanguageDto[] Languages { get; set; } = [];
    public DictionaryDto[] Dictionaries { get; set; } = [];
    public TranslationDto[] Translations { get; set; } = [];
}
