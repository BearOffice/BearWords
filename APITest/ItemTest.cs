using APITest.Helpers;
using BearMarkupLanguage;
using BearWordsAPI.Shared.Data;
using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.Shared.DTOs;
using BearWordsAPI.RequestHandler;
using BearWordsAPI.Services;
using BearWordsAPI.Shared.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using System.Security.Claims;

namespace APITest;

public class ItemTest
{
    private readonly ConfigService _configService;
    private readonly BearWordsContext _context;
    private readonly ClaimsPrincipal _principal;
    private readonly MockDateTimeService _dateTimeService;
    private readonly UUIDGenerator _uuid;

    public ItemTest()
    {
        var ml = new BearML();
        ml.AddKeyValue("issuer_key", "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08");
        _configService = new ConfigService(ml);

        _dateTimeService = new MockDateTimeService();
        _uuid = new UUIDGenerator();

        _context = new MockDb().CreateDbContext();
        // `admin` created at t1.
        _context.Users.Add(new User
        {
            UserName = "admin",
            CreatedAt = Timestamp.GenerateDatetimeLong(1),
        });
        _context.SaveChanges();

        _principal = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, "User")
            ]));
    }

    [Fact]
    public async Task GetItemsById()
    {
        await RootHandler.PostRegister(new RegisterRequest("client-a"), _principal, _context);

        await AddMockData(_context, _uuid, 1);

        _dateTimeService.Next(2);
        var items = await ItemsHandler.GetItems(_principal, _context);
        var itemsGlobal = await ItemsHandler.GetItemsGlobal(_context);

        AssertItemsCounts(items.Value!, itemsGlobal.Value!, 2, 1, 1, 1, 1, 1, 1, 1, 1);

        var dicId = _context.Dictionaries.First().WordId;
        var dicResult = await ItemsHandler.GetItemsDictionaryById(dicId, _context);
        Assert.Equal(_context.Dictionaries.First().Pronounce,
            ((Ok<DictionaryDto>)dicResult.Result).Value!.Pronounce);

        var transId = _context.Translations.First().TranslationId;
        var transResult = await ItemsHandler.GetItemsTranslationById(transId, _context);
        Assert.Equal(_context.Translations.First().TranslationText,
            ((Ok<TranslationDto>)transResult.Result).Value!.TranslationText);

        var phraseId = _context.Phrases.First().PhraseId;
        var phraseResult = await ItemsHandler.GetItemsPhraseById(phraseId, _principal, _context);
        Assert.Equal(_context.Phrases.First().PhraseText,
            ((Ok<PhraseDto>)phraseResult.Result).Value!.PhraseText);

        var phraseTagId = _context.PhraseTags.First().PhraseTagId;
        var phraseTagResult = await ItemsHandler.GetItemsPhraseTagById(phraseTagId, _principal, _context);
        Assert.Equal(_context.PhraseTags.First().PhraseId,
            ((Ok<PhraseTagDto>)phraseTagResult.Result).Value!.PhraseId);

        var bookmarkId = _context.Bookmarks.First().BookmarkId;
        var bookmarkResult = await ItemsHandler.GetItemsBookmarkById(bookmarkId, _principal, _context);
        Assert.Equal(_context.Bookmarks.First().Note,
            ((Ok<BookmarkDto>)bookmarkResult.Result).Value!.Note);

        var tagCatId = _context.TagCategories.First().TagCategoryId;
        var tagCatResult = await ItemsHandler.GetItemsTagCategoryById(tagCatId, _principal, _context);
        Assert.Equal(_context.TagCategories.First().Description,
            ((Ok<TagCategoryDto>)tagCatResult.Result).Value!.Description);

        var tagId = _context.Tags.First().TagId;
        var tagResult = await ItemsHandler.GetItemsTagById(tagId, _principal, _context);
        Assert.Equal(_context.Tags.First().Description,
            ((Ok<TagDto>)tagResult.Result).Value!.Description);

        var bookmarkTagId = _context.BookmarkTags.First().BookmarkTagId;
        var bookmarkTagResult = await ItemsHandler.GetItemsBookmarkTagById(bookmarkTagId, _principal, _context);
        Assert.Equal(_context.BookmarkTags.First().BookmarkId,
            ((Ok<BookmarkTagDto>)bookmarkTagResult.Result).Value!.BookmarkId);
    }

    [Fact]
    public async Task GetItemsByIdIfNotFound()
    {
        await RootHandler.PostRegister(new RegisterRequest("client-a"), _principal, _context);

        await AddMockData(_context, _uuid, 1);

        _dateTimeService.Next(2);
        var items = await ItemsHandler.GetItems(_principal, _context);
        var itemsGlobal = await ItemsHandler.GetItemsGlobal(_context);

        AssertItemsCounts(items.Value!, itemsGlobal.Value!, 2, 1, 1, 1, 1, 1, 1, 1, 1);

        var dicResult = await ItemsHandler.GetItemsDictionaryById(3, _context);
        Assert.IsType<NotFound>(dicResult.Result);

        var transResult = await ItemsHandler.GetItemsTranslationById(3, _context);
        Assert.IsType<NotFound>(transResult.Result);

        var phraseResult = await ItemsHandler.GetItemsPhraseById("123", _principal, _context);
        Assert.IsType<NotFound>(phraseResult.Result);

        var phraseTagResult = await ItemsHandler.GetItemsPhraseTagById("123", _principal, _context);
        Assert.IsType<NotFound>(phraseTagResult.Result);

        var bookmarkResult = await ItemsHandler.GetItemsBookmarkById("123", _principal, _context);
        Assert.IsType<NotFound>(bookmarkResult.Result);

        var tagCatResult = await ItemsHandler.GetItemsTagCategoryById("123", _principal, _context);
        Assert.IsType<NotFound>(tagCatResult.Result);

        var tagResult = await ItemsHandler.GetItemsTagById("123", _principal, _context);
        Assert.IsType<NotFound>(tagResult.Result);

        var bookmarkTagResult = await ItemsHandler.GetItemsBookmarkTagById("123", _principal, _context);
        Assert.IsType<NotFound>(bookmarkTagResult.Result);
    }

    private static async Task AddMockData(BearWordsContext context, IUUIDGenerator uuid, int timestamp)
    {
        await context.Languages.AddRangeAsync(
            new Language { LanguageCode = "EN", LanguageName = "English" },
            new Language { LanguageCode = "JA", LanguageName = "Japanese" }
            );
        await context.Dictionaries.AddRangeAsync(
            new Dictionary
            {
                WordId = 1,
                Word = "Hello",
                Pronounce = "heh·low",
                SourceLanguage = "EN",
                ModifiedAt = Timestamp.GenerateDatetimeLong(timestamp),
            });
        await context.Translations.AddRangeAsync(
            new Translation
            {
                WordId = 1,
                TranslationId = 1,
                TargetLanguage = "JA",
                TranslationText = "kon ni chi wa",
                ModifiedAt = Timestamp.GenerateDatetimeLong(timestamp),
            });
        var phraseId = uuid.Generate();
        await context.Phrases.AddRangeAsync(
            new Phrase
            {
                PhraseId = phraseId,
                PhraseLanguage = "EN",
                PhraseText = "how are you",
                Note = "greeting",
                UserName = "admin",
                ModifiedAt = Timestamp.GenerateDatetimeLong(timestamp),
            });
        var bookmarkId = uuid.Generate();
        await context.Bookmarks.AddRangeAsync(
            new Bookmark
            {
                BookmarkId = bookmarkId,
                WordId = 1,
                UserName = "admin",
                Note = "basic word",
                ModifiedAt = Timestamp.GenerateDatetimeLong(timestamp),
            },
            new Bookmark
            {
                BookmarkId = uuid.Generate(),
                WordId = 1,
                UserName = "other",
                Note = "null",
                ModifiedAt = Timestamp.GenerateDatetimeLong(timestamp),
            });

        var tagCatId = uuid.Generate();
        await context.TagCategories.AddRangeAsync(
            new TagCategory
            {
                TagCategoryId = tagCatId,
                CategoryName = "level",
                UserName = "admin",
                Description = "level of words",
                ModifiedAt = Timestamp.GenerateDatetimeLong(timestamp),
            },
            new TagCategory
            {
                TagCategoryId = uuid.Generate(),
                CategoryName = "other cat",
                UserName = "other",
                Description = "other desc",
                ModifiedAt = Timestamp.GenerateDatetimeLong(timestamp),
            });

        var tagId = uuid.Generate();
        await context.Tags.AddRangeAsync(
            new Tag
            {
                TagId = tagId,
                TagName = "starter",
                Description = "Starter level",
                TagCategoryId = tagCatId,
                ModifiedAt = Timestamp.GenerateDatetimeLong(timestamp),
            });
        await context.BookmarkTags.AddRangeAsync(
            new BookmarkTag
            {
                BookmarkTagId = uuid.Generate(),
                BookmarkId = bookmarkId,
                TagId = tagId,
                ModifiedAt = Timestamp.GenerateDatetimeLong(timestamp),
            });
        await context.PhraseTags.AddRangeAsync(
            new PhraseTag
            {
                PhraseTagId = uuid.Generate(),
                PhraseId = phraseId,
                TagId = tagId,
                ModifiedAt = Timestamp.GenerateDatetimeLong(timestamp),
            });

        await context.SaveChangesAsync();
    }

    private static void AssertPullDtoItemCounts(SyncPullDto dto, int langs = 0, int phrases = 0, int dicts = 0, int trans = 0,
        int bookmarks = 0, int bookmarkTags = 0, int tagCats = 0, int tags = 0)
    {
        Assert.Equal(langs, dto.Languages.Length);
        Assert.Equal(phrases, dto.Phrases.Length);
        Assert.Equal(dicts, dto.Dictionaries.Length);
        Assert.Equal(trans, dto.Translations.Length);
        Assert.Equal(bookmarks, dto.Bookmarks.Length);
        Assert.Equal(bookmarkTags, dto.BookmarkTags.Length);
        Assert.Equal(tagCats, dto.TagCategories.Length);
        Assert.Equal(tags, dto.Tags.Length);
    }

    private static void AssertItemsCounts(ItemsDto itemsDto, ItemsGlobalDto itemsGlobalDto,
        int langs = 0, int dicts = 0, int trans = 0, int phrases = 0, int phraseTags = 0,
        int bookmarks = 0, int bookmarkTags = 0, int tagCats = 0, int tags = 0)
    {
        Assert.Equal(langs, itemsGlobalDto.Languages.Length);
        Assert.Equal(dicts, itemsGlobalDto.Dictionaries.Length);
        Assert.Equal(trans, itemsGlobalDto.Translations.Length);
        Assert.Equal(phrases, itemsDto.Phrases.Length);
        Assert.Equal(phraseTags, itemsDto.PhraseTags.Length);
        Assert.Equal(bookmarks, itemsDto.Bookmarks.Length);
        Assert.Equal(bookmarkTags, itemsDto.BookmarkTags.Length);
        Assert.Equal(tagCats, itemsDto.TagCategories.Length);
        Assert.Equal(tags, itemsDto.Tags.Length);
    }
}
