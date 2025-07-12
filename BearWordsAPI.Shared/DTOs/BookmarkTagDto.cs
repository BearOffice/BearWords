using BearWordsAPI.Shared.Data.Models;

namespace BearWordsAPI.Shared.DTOs;

public class BookmarkTagDto
{
    public string BookmarkTagId { get; set; } = null!;
    public string BookmarkId { get; set; } = null!;
    public string TagId { get; set; } = null!;
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }

    public BookmarkTagDto() { }

    public BookmarkTagDto(BookmarkTag bookmarkTag)
    {
        BookmarkTagId = bookmarkTag.BookmarkTagId;
        BookmarkId = bookmarkTag.BookmarkId;
        TagId = bookmarkTag.TagId;
        ModifiedAt = bookmarkTag.ModifiedAt;
        DeleteFlag = bookmarkTag.DeleteFlag;
    }

    public BookmarkTag ToEntity()
    {
        return new BookmarkTag
        {
            BookmarkTagId = BookmarkTagId,
            BookmarkId = BookmarkId,
            TagId = TagId,
            ModifiedAt = ModifiedAt,
            DeleteFlag = DeleteFlag,
        };
    }
}
