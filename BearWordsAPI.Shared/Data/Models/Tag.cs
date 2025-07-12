using BearWordsAPI.Shared.Helpers;

namespace BearWordsAPI.Shared.Data.Models;

public partial class Tag : ITimestamps, ISoftDeletable
{
    public string TagId { get; set; } = null!;
    public string TagName { get; set; } = null!;
    public string TagCategoryId { get; set; } = null!;
    public string? Description { get; set; }
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }

    [CascadeSoftDelete(OnDelete = false, OnRestore = true)]
    public virtual TagCategory TagCategory { get; set; } = null!;

    [CascadeSoftDelete]
    public virtual ICollection<BookmarkTag> BookmarkTags { get; set; } = [];

    [CascadeSoftDelete]
    public virtual ICollection<PhraseTag> PhraseTags { get; set; } = [];
}
