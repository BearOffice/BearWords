namespace BearWordsAPI.Shared.DTOs;

public class SyncPushDto
{
    public PhraseDto[] Phrases { get; set; } = [];
    public PhraseTagDto[] PhraseTags { get; set; } = [];
    public BookmarkDto[] Bookmarks { get; set; } = [];
    public BookmarkTagDto[] BookmarkTags { get; set; } = [];
    public TagCategoryDto[] TagCategories { get; set; } = [];
    public TagDto[] Tags { get; set; } = [];
}
