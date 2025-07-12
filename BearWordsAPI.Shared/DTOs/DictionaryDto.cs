using BearWordsAPI.Shared.Data.Models;

namespace BearWordsAPI.Shared.DTOs;

public class DictionaryDto
{
    public int WordId { get; set; }
    public string Word { get; set; } = null!;
    public string SourceLanguage { get; set; } = null!;
    public string? Pronounce { get; set; }
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }

    public DictionaryDto() { }

    public DictionaryDto(Dictionary dictionary)
    {
        WordId = dictionary.WordId;
        Word = dictionary.Word;
        SourceLanguage = dictionary.SourceLanguage;
        Pronounce = dictionary.Pronounce;
        ModifiedAt = dictionary.ModifiedAt;
        DeleteFlag = dictionary.DeleteFlag;
    }

    public Dictionary ToEntity()
    {
        return new Dictionary
        {
            WordId = WordId,
            Word = Word,
            SourceLanguage = SourceLanguage,
            Pronounce = Pronounce,
            ModifiedAt = ModifiedAt,
            DeleteFlag = DeleteFlag
        };
    }
}
