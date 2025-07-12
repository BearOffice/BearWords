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

public class AuthTest
{
    private readonly ConfigService _configService;
    private readonly BearWordsContext _context;
    private readonly ClaimsPrincipal _principal;
    private readonly MockDateTimeService _dateTimeService;
    private readonly UUIDGenerator _uuid;

    public AuthTest()
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
    public async Task RegisterAndPullEmpty()
    {
        var resultReregister = await RootHandler.PostReregister(
            new RegisterRequest("client-a"), _principal, _context);
        Assert.IsType<NotFound>(resultReregister.Result);

        var result = await RootHandler.PostRegister(new RegisterRequest("client-a"), _principal, _context);
        Assert.IsType<Created>(result.Result);

        result = await RootHandler.PostRegister(new RegisterRequest("client-a"), _principal, _context);
        Assert.IsType<Conflict>(result.Result);

        resultReregister = await RootHandler.PostReregister(
            new RegisterRequest("client-a"), _principal, _context);
        Assert.IsType<Ok>(resultReregister.Result);

        // client last sync = t0 -> t2, server last sync = t3
        _dateTimeService.Next(3);
        var pullResult = await SyncsHandler.PostSyncPull(
            "client-a", new SyncPullRequest(Timestamp.GenerateDatetimeString(0)),
            _principal, _context, _dateTimeService);

        Assert.NotNull(pullResult.Value);
        AssertPullDtoItemCounts(pullResult.Value.PullDto);
    }

    [Fact]
    public async Task RegisterAndPullNotEmpty()
    {
        await RegisterAndPullEmpty();

        // server modify data at t4
        await AddMockData(_context, _uuid, 4);

        // client last sync = t2 -> t5, server last sync = t6
        _dateTimeService.Next(6);
        var pullResult = await SyncsHandler.PostSyncPull(
            "client-a", new SyncPullRequest(Timestamp.GenerateDatetimeString(2)),
            _principal, _context, _dateTimeService);

        Assert.NotNull(pullResult.Value);
        AssertPullDtoItemCounts(pullResult.Value.PullDto, 2, 1, 1, 1, 1, 1, 1, 1, 1);
        Assert.Equal("starter", _context.Tags.First().TagName);


        // server modify data at t7
        _context.Tags.First().TagName = "intermediate";
        _context.Tags.First().ModifiedAt = Timestamp.GenerateDatetimeLong(7);
        await _context.SaveChangesAsync();


        // client last sync = t5 -> t8, server last sync = t9
        _dateTimeService.Next(9);
        pullResult = await SyncsHandler.PostSyncPull(
            "client-a", new SyncPullRequest(Timestamp.GenerateDatetimeString(5)),
            _principal, _context, _dateTimeService);

        Assert.NotNull(pullResult.Value);
        AssertPullDtoItemCounts(pullResult.Value.PullDto, langs: 2, tags: 1);
        Assert.Equal("intermediate", _context.Tags.First().TagName);
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

    private static void AssertPullDtoItemCounts(SyncPullDto dto, int langs = 0,
        int dicts = 0, int trans = 0, int phrases = 0, int phraseTags = 0,
        int bookmarks = 0, int bookmarkTags = 0, int tagCats = 0, int tags = 0)
    {
        Assert.Equal(langs, dto.Languages.Length);
        Assert.Equal(dicts, dto.Dictionaries.Length);
        Assert.Equal(trans, dto.Translations.Length);
        Assert.Equal(phrases, dto.Phrases.Length);
        Assert.Equal(phraseTags, dto.PhraseTags.Length);
        Assert.Equal(bookmarks, dto.Bookmarks.Length);
        Assert.Equal(bookmarkTags, dto.BookmarkTags.Length);
        Assert.Equal(tagCats, dto.TagCategories.Length);
        Assert.Equal(tags, dto.Tags.Length);
    }
}
