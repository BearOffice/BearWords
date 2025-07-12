using BearWordsAPI.Shared.Data.Models;

namespace BearWordsAPI.Shared.DTOs;

public class PhraseDto
{
    public string PhraseId { get; set; } = null!;
    public string PhraseText { get; set; } = null!;
    public string PhraseLanguage { get; set; } = null!;
    public string? Note { get; set; }
    public string UserName { get; set; } = null!;
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }

    public PhraseDto() { }

    public PhraseDto(Phrase phrase)
    {
        PhraseId = phrase.PhraseId;
        PhraseText = phrase.PhraseText;
        PhraseLanguage = phrase.PhraseLanguage;
        Note = phrase.Note;
        UserName = phrase.UserName;
        ModifiedAt = phrase.ModifiedAt;
        DeleteFlag = phrase.DeleteFlag;
    }

    public Phrase ToEntity()
    {
        return new Phrase
        {
            PhraseId = PhraseId,
            PhraseText = PhraseText,
            PhraseLanguage = PhraseLanguage,
            Note = Note,
            UserName = UserName,
            ModifiedAt = ModifiedAt,
            DeleteFlag = DeleteFlag
        };
    }
}
