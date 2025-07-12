using BearWordsAPI.Shared.Helpers;

namespace BearWordsAPI.Shared.Data.Models;

public partial class Phrase : ITimestamps, ISoftDeletable, IUserData
{
    public string PhraseId { get; set; } = null!;
    public string PhraseText { get; set; } = null!;
    public string PhraseLanguage { get; set; } = null!;
    public string? Note { get; set; }
    public string UserName { get; set; } = null!;
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }
    
    public virtual Language PhraseLanguageNavigation { get; set; } = null!;
    public virtual User UserNameNavigation { get; set; } = null!;

    [CascadeSoftDelete]
    public virtual ICollection<PhraseTag> PhraseTags { get; set; } = [];
}
