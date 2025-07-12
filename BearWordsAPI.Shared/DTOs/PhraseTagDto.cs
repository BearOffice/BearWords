using BearWordsAPI.Shared.Data.Models;

namespace BearWordsAPI.Shared.DTOs;

public class PhraseTagDto
{
    public string PhraseTagId { get; set; } = null!;
    public string PhraseId { get; set; } = null!;
    public string TagId { get; set; } = null!;
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }

    public PhraseTagDto() { }

    public PhraseTagDto(PhraseTag phraseTag)
    {
        PhraseTagId = phraseTag.PhraseTagId;
        PhraseId = phraseTag.PhraseId;
        TagId = phraseTag.TagId;
        ModifiedAt = phraseTag.ModifiedAt;
        DeleteFlag = phraseTag.DeleteFlag;
    }

    public PhraseTag ToEntity()
    {
        return new PhraseTag
        {
            PhraseTagId = PhraseTagId,
            PhraseId = PhraseId,
            TagId = TagId,
            ModifiedAt = ModifiedAt,
            DeleteFlag = DeleteFlag,
        };
    }
}
