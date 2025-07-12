using BearWordsAPI.Shared.Helpers;

namespace BearWordsAPI.Shared.Data.Models;

public partial class Bookmark : ITimestamps, ISoftDeletable, IUserData
{
    public string BookmarkId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public int WordId { get; set; }
    public string? Note { get; set; }
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }

    public virtual User UserNameNavigation { get; set; } = null!;
    public virtual Dictionary Word { get; set; } = null!;

    [CascadeSoftDelete]
    public virtual ICollection<BookmarkTag> BookmarkTags { get; set; } = [];
}
