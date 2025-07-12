using BearWordsAPI.Shared.Data;
using BearWordsAPI.Shared.DTOs;
using BearWordsAPI.Shared.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BearWordsAPI.RequestHandler;


public static class ItemsHandler
{
    public static async Task<Ok<ItemsDto>> GetItems(ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;

        var phraseTask = db.Phrases
            .AsNoTracking()
            .WhereUser(userName)
            .Select(item => new PhraseDto(item))
            .ToArrayAsync();

        var phraseTagTask = db.PhraseTags
            .AsNoTracking()
            .WhereUser(userName)
            .Select(item => new PhraseTagDto(item))
            .ToArrayAsync();

        var bookmarkTask = db.Bookmarks
            .AsNoTracking()
            .WhereUser(userName)
            .Select(item => new BookmarkDto(item))
            .ToArrayAsync();

        var bookmarkTagTask = db.BookmarkTags
            .AsNoTracking()
            .WhereUser(userName)
            .Select(item => new BookmarkTagDto(item))
            .ToArrayAsync();

        var tagCategoryTask = db.TagCategories
            .AsNoTracking()
            .WhereUser(userName)
            .Select(item => new TagCategoryDto(item))
            .ToArrayAsync();

        var tagTask = db.Tags
            .AsNoTracking()
            .WhereUser(userName)
            .Select(item => new TagDto(item))
            .ToArrayAsync();

        await Task.WhenAll(phraseTask, phraseTagTask, bookmarkTask, bookmarkTagTask, tagCategoryTask, tagTask);

        var itemsDto = new ItemsDto
        {
            Phrases = phraseTask.Result,
            PhraseTags = phraseTagTask.Result,
            Bookmarks = bookmarkTask.Result,
            BookmarkTags = bookmarkTagTask.Result,
            TagCategories = tagCategoryTask.Result,
            Tags = tagTask.Result,
        };

        return TypedResults.Ok(itemsDto);
    }

    public static async Task<Ok<ItemsGlobalDto>> GetItemsGlobal(BearWordsContext db)
    {
        var languageTask = db.Languages
            .AsNoTracking()
            .Select(item => new LanguageDto(item))
            .ToArrayAsync();

        var dictionaryTask = db.Dictionaries
            .AsNoTracking()
            .Select(item => new DictionaryDto(item))
            .ToArrayAsync();

        var translationTask = db.Translations
            .AsNoTracking()
            .Select(item => new TranslationDto(item))
            .ToArrayAsync();

        await Task.WhenAll(languageTask, dictionaryTask, translationTask);

        var itemsDto = new ItemsGlobalDto
        {
            Languages = languageTask.Result,
            Dictionaries = dictionaryTask.Result,
            Translations = translationTask.Result,
        };

        return TypedResults.Ok(itemsDto);
    }

    public static async Task<Ok<LanguageDto[]>> GetItemsLanguages(BearWordsContext db)
    {
        var languages = await db.Languages
            .AsNoTracking()
            .Select(item => new LanguageDto(item))
            .ToArrayAsync();

        return TypedResults.Ok(languages);
    }

    public static async Task<Ok<DictionaryDto[]>> GetItemsDictionaries(BearWordsContext db)
    {
        var dictionaries = await db.Dictionaries
            .AsNoTracking()
            .Select(item => new DictionaryDto(item))
            .ToArrayAsync();

        return TypedResults.Ok(dictionaries);
    }

    public static async Task<Ok<TranslationDto[]>> GetItemsTranslations(BearWordsContext db)
    {
        var translations = await db.Translations
            .AsNoTracking()
            .Select(item => new TranslationDto(item))
            .ToArrayAsync();

        return TypedResults.Ok(translations);
    }

    public static async Task<Ok<PhraseDto[]>> GetItemsPhrases(ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;

        var phrases = await db.Phrases
            .AsNoTracking()
            .WhereUser(userName)
            .Select(item => new PhraseDto(item))
            .ToArrayAsync();

        return TypedResults.Ok(phrases);
    }

    public static async Task<Ok<PhraseTagDto[]>> GetItemsPhraseTags(ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;

        var phraseTags = await db.PhraseTags
            .AsNoTracking()
            .WhereUser(userName)
            .Select(item => new PhraseTagDto(item))
            .ToArrayAsync();

        return TypedResults.Ok(phraseTags);
    }

    public static async Task<Ok<BookmarkDto[]>> GetItemsBookmarks(ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;

        var bookmarks = await db.Bookmarks
            .AsNoTracking()
            .WhereUser(userName)
            .Select(item => new BookmarkDto(item))
            .ToArrayAsync();

        return TypedResults.Ok(bookmarks);
    }

    public static async Task<Ok<BookmarkTagDto[]>> GetItemsBookmarkTags(ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;

        var bookmarkTags = await db.BookmarkTags
            .AsNoTracking()
            .WhereUser(userName)
            .Select(item => new BookmarkTagDto(item))
            .ToArrayAsync();

        return TypedResults.Ok(bookmarkTags);
    }

    public static async Task<Ok<TagCategoryDto[]>> GetItemsTagCategories(ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;

        var tagCategories = await db.TagCategories
            .AsNoTracking()
            .WhereUser(userName)
            .Select(item => new TagCategoryDto(item))
            .ToArrayAsync();

        return TypedResults.Ok(tagCategories);
    }

    public static async Task<Ok<TagDto[]>> GetItemsTags(ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;

        var tags = await db.Tags
            .AsNoTracking()
            .WhereUser(userName)
            .Select(item => new TagDto(item))
            .ToArrayAsync();

        return TypedResults.Ok(tags);
    }

    public static async Task<Results<Ok<DictionaryDto>, NotFound>> GetItemsDictionaryById(
        [FromRoute] int id, BearWordsContext db)
    {
        var dictionary = await db.Dictionaries
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.WordId == id);

        if (dictionary is null) return TypedResults.NotFound();
        return TypedResults.Ok(new DictionaryDto(dictionary));
    }

    public static async Task<Results<Ok<TranslationDto>, NotFound>> GetItemsTranslationById(
        [FromRoute] int id, BearWordsContext db)
    {
        var translation = await db.Translations
            .AsNoTracking()
            .SingleOrDefaultAsync(item => item.TranslationId == id);

        if (translation is null) return TypedResults.NotFound();
        return TypedResults.Ok(new TranslationDto(translation));
    }

    public static async Task<Results<Ok<PhraseDto>, NotFound>> GetItemsPhraseById(
        [FromRoute] string id, ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;

        var phrase = await db.Phrases
            .AsNoTracking()
            .WhereUser(userName)
            .SingleOrDefaultAsync(item => item.PhraseId == id);

        if (phrase is null) return TypedResults.NotFound();
        return TypedResults.Ok(new PhraseDto(phrase));
    }

    public static async Task<Results<Ok<PhraseTagDto>, NotFound>> GetItemsPhraseTagById(
        [FromRoute] string id, ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;

        var phraseTag = await db.PhraseTags
            .AsNoTracking()
            .WhereUser(userName)
            .SingleOrDefaultAsync(item => item.PhraseTagId == id);

        if (phraseTag is null) return TypedResults.NotFound();
        return TypedResults.Ok(new PhraseTagDto(phraseTag));
    }

    public static async Task<Results<Ok<BookmarkDto>, NotFound>> GetItemsBookmarkById(
        [FromRoute] string id, ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;

        var bookmark = await db.Bookmarks
            .AsNoTracking()
            .WhereUser(userName)
            .SingleOrDefaultAsync(item => item.BookmarkId == id);

        if (bookmark is null) return TypedResults.NotFound();
        return TypedResults.Ok(new BookmarkDto(bookmark));
    }

    public static async Task<Results<Ok<BookmarkTagDto>, NotFound>> GetItemsBookmarkTagById(
         [FromRoute] string id, ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;

        var bookmarkTag = await db.BookmarkTags
            .AsNoTracking()
            .WhereUser(userName)
            .SingleOrDefaultAsync(item => item.BookmarkTagId == id);

        if (bookmarkTag is null) return TypedResults.NotFound();
        return TypedResults.Ok(new BookmarkTagDto(bookmarkTag));
    }

    public static async Task<Results<Ok<TagCategoryDto>, NotFound>> GetItemsTagCategoryById(
        [FromRoute] string id, ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;

        var tagCategory = await db.TagCategories
            .AsNoTracking()
            .WhereUser(userName)
            .SingleOrDefaultAsync(item => item.TagCategoryId == id);

        if (tagCategory is null) return TypedResults.NotFound();
        return TypedResults.Ok(new TagCategoryDto(tagCategory));
    }

    public static async Task<Results<Ok<TagDto>, NotFound>> GetItemsTagById(
         [FromRoute] string id, ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;

        var tag = await db.Tags
            .AsNoTracking()
            .WhereUser(userName)
            .SingleOrDefaultAsync(item => item.TagId == id);

        if (tag is null) return TypedResults.NotFound();
        return TypedResults.Ok(new TagDto(tag));
    }
}
