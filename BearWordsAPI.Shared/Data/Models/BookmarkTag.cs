namespace BearWordsAPI.Shared.Data.Models;

public partial class BookmarkTag : ITimestamps, ISoftDeletable
{
    public string BookmarkTagId { get; set; } = null!;
    public string BookmarkId { get; set; } = null!;
    public string TagId { get; set; } = null!;
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }

    public virtual Bookmark Bookmark { get; set; } = null!;
    public virtual Tag Tag { get; set; } = null!;
}
