using BearWordsAPI.Shared.Data.Models;

namespace BearWordsAPI.Shared.DTOs;

public class TranslationDto
{
    public int TranslationId { get; set; }
    public int WordId { get; set; }
    public string TargetLanguage { get; set; } = null!;
    public string TranslationText { get; set; } = null!;
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }

    public TranslationDto() { }

    public TranslationDto(Translation translation)
    {
        TranslationId = translation.TranslationId;
        WordId = translation.WordId;
        TargetLanguage = translation.TargetLanguage;
        TranslationText = translation.TranslationText;
        ModifiedAt = translation.ModifiedAt;
        DeleteFlag = translation.DeleteFlag;
    }

    public Translation ToEntity()
    {
        return new Translation
        {
            TranslationId = TranslationId,
            WordId = WordId,
            TargetLanguage = TargetLanguage,
            TranslationText = TranslationText,
            ModifiedAt = ModifiedAt,
            DeleteFlag = DeleteFlag
        };
    }
}
