namespace BearWordsAPI.Shared.Data.Models;

public partial class PhraseTag : ITimestamps, ISoftDeletable
{
    public string PhraseTagId { get; set; } = null!;
    public string PhraseId { get; set; } = null!;
    public string TagId { get; set; } = null!;
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }
    
    public virtual Phrase Phrase { get; set; } = null!;
    public virtual Tag Tag { get; set; } = null!;
}
