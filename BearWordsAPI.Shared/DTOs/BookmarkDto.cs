using BearWordsAPI.Shared.Data.Models;

namespace BearWordsAPI.Shared.DTOs;

public class BookmarkDto
{
    public string BookmarkId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public int WordId { get; set; }
    public string? Note { get; set; }
    public long ModifiedAt { get; set; }
    public bool DeleteFlag { get; set; }

    public BookmarkDto() { }

    public BookmarkDto(Bookmark bookmark)
    {
        BookmarkId = bookmark.BookmarkId;
        UserName = bookmark.UserName;
        WordId = bookmark.WordId;
        Note = bookmark.Note;
        ModifiedAt = bookmark.ModifiedAt;
        DeleteFlag = bookmark.DeleteFlag;
    }

    public Bookmark ToEntity()
    {
        return new Bookmark
        {
            BookmarkId = BookmarkId,
            UserName = UserName,
            WordId = WordId,
            Note = Note,
            ModifiedAt = ModifiedAt,
            DeleteFlag = DeleteFlag
        };
    }
}
