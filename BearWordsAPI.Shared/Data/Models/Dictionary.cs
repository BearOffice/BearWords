using BearWordsAPI.Shared.Helpers;

namespace BearWordsAPI.Shared.Data.Models;

public partial class Dictionary : ITimestamps, ISoftDeletable
{
    public int WordId { get; set; }
    public string Word { get; set; } = null!;
    public string SourceLanguage { get; set; } = null!;
    public string? Pronounce { get; set; }
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }

    public virtual Language SourceLanguageNavigation { get; set; } = null!;

    [CascadeSoftDelete]
    public virtual ICollection<Bookmark> Bookmarks { get; set; } = [];

    [CascadeSoftDelete]
    public virtual ICollection<Translation> Translations { get; set; } = [];
}
