using BearWordsAPI.Shared.Helpers;

namespace BearWordsAPI.Shared.Data.Models;

public partial class TagCategory : ITimestamps, ISoftDeletable, IUserData
{
    public string TagCategoryId { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string? Description { get; set; }
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }

    public virtual User UserNameNavigation { get; set; } = null!;

    [CascadeSoftDelete]
    public virtual ICollection<Tag> Tags { get; set; } = [];
}
