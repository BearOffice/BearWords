namespace BearWordsAPI.Shared.Data.Models;

public partial class User : IUserData
{
    public string UserName { get; set; } = null!;
    public long CreatedAt { get; set; }

    public virtual ICollection<Bookmark> Bookmarks { get; set; } = [];
    public virtual ICollection<ConflictLog> ConflictLogs { get; set; } = [];
    public virtual ICollection<Phrase> Phrases { get; set; } = [];
    public virtual ICollection<Sync> Syncs { get; set; } = [];
    public virtual ICollection<TagCategory> TagCategories { get; set; } = [];
}
