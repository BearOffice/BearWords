using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.Shared.Helpers;
using BearWordsMaui.Helpers;
using BearWordsMaui.Services.DbServices.DataItems;
using Microsoft.EntityFrameworkCore;

namespace BearWordsMaui.Services.DbServices;

public class SearchService
{
    private readonly IDbContextService _dbContextService;
    private BearWordsContext Context => _dbContextService.GetDbContext();

    public SearchService(IDbContextService dbContextService)
    {
        _dbContextService = dbContextService;
    }

    public Task<IEnumerable<SearchResult>> FuzzySearchAsync(
        string keyword, SearchItemType itemType, string? filter = null,
        int maxResults = 50, int dbRetrieveLimit = 100)
    {
        keyword = keyword.Trim().ToLower();
        if (filter is not null) filter = filter.Trim().ToLower();

        if (string.IsNullOrWhiteSpace(keyword))
        {
            return Task.FromResult(itemType switch
            {
                SearchItemType.WordInTag => StandardWordInTagSearch(keyword, filter!, maxResults, dbRetrieveLimit),
                SearchItemType.WordInTagCategory => StandardWordInTagCatSearch(
                    keyword, filter!, maxResults, dbRetrieveLimit),
                SearchItemType.PhraseInTag => StandardPhraseInTagSearch(keyword, filter!, maxResults, dbRetrieveLimit),
                SearchItemType.PhraseInTagCategory => StandardPhraseInTagCatSearch(
                    keyword, filter!, maxResults, dbRetrieveLimit),
                _ => []
            });
        }

        if (keyword.Length <= 2)
        {
            return Task.FromResult(itemType switch
            {
                SearchItemType.Word => TieredWordSearch(keyword, maxResults, dbRetrieveLimit),
                SearchItemType.BookmarkedWord => TieredBookmarkedWordSearch(keyword, maxResults, dbRetrieveLimit),
                SearchItemType.Phrase => TieredPhraseSearch(keyword, maxResults, dbRetrieveLimit),
                SearchItemType.WordInTag => StandardWordInTagSearch(keyword, filter!, maxResults, dbRetrieveLimit),
                SearchItemType.WordInTagCategory => StandardWordInTagCatSearch(
                    keyword, filter!, maxResults, dbRetrieveLimit),
                SearchItemType.PhraseInTag => StandardPhraseInTagSearch(keyword, filter!, maxResults, dbRetrieveLimit),
                SearchItemType.PhraseInTagCategory => StandardPhraseInTagCatSearch(
                    keyword, filter!, maxResults, dbRetrieveLimit),
                _ => throw new NotImplementedException()
            });
        }

        return Task.FromResult(itemType switch
        {
            SearchItemType.Word => StandardWordSearch(keyword, maxResults, dbRetrieveLimit),
            SearchItemType.BookmarkedWord => StandardBookmarkedWordSearch(keyword, maxResults, dbRetrieveLimit),
            SearchItemType.Phrase => StandardPhraseSearch(keyword, maxResults, dbRetrieveLimit),
            SearchItemType.WordInTag => StandardWordInTagSearch(keyword, filter!, maxResults, dbRetrieveLimit),
            SearchItemType.WordInTagCategory => StandardWordInTagCatSearch(
                keyword, filter!, maxResults, dbRetrieveLimit),
            SearchItemType.PhraseInTag => StandardPhraseInTagSearch(keyword, filter!, maxResults, dbRetrieveLimit),
            SearchItemType.PhraseInTagCategory => StandardPhraseInTagCatSearch(
                keyword, filter!, maxResults, dbRetrieveLimit),
            _ => throw new NotImplementedException()
        });
    }

    private IEnumerable<SearchResult> TieredWordSearch(
        string keyword, int maxResults, int dbRetrieveLimit)
    {
        var results = new List<SearchResult>();

        // Exact matches
        var exactDictionaries = Context.Dictionaries
            .AsNoTracking()
            .WhereNotDeleted()
            .Where(d => d.Word.ToLower() == keyword
                        || d.Pronounce!.ToLower() == keyword)
            .Take(dbRetrieveLimit)
            .Include(d => d.Bookmarks.Where(b => !b.DeleteFlag))
                .ThenInclude(b => b.BookmarkTags.Where(bt => !bt.DeleteFlag))
                .ThenInclude(bt => bt.Tag)
            .AsEnumerable()
            .Select(d => new SearchResult
            {
                Item = d,
                Type = SearchItemType.Word,
                Similarity = 100,
                IsBookmarked = d.Bookmarks.Count != 0,
                TagNames = d.Bookmarks
                    .Select(b => b.BookmarkTags)
                    .SelectMany(bt => bt)
                    .Select(bt => bt.Tag.TagName)
            });
        results.AddRange(exactDictionaries);

        // Starts with matches
        if (results.Count < maxResults)
        {
            var remainingSlots = maxResults - results.Count;
            var startsWithDictionaries = Context.Dictionaries
                .AsNoTracking()
                .WhereNotDeleted()
                .Where(d => d.Word.ToLower().StartsWith(keyword) && d.Word.ToLower() != keyword
                            || d.Pronounce!.ToLower().StartsWith(keyword) && d.Pronounce!.ToLower() != keyword)
                .Take(remainingSlots)
                .Include(d => d.Bookmarks.Where(b => !b.DeleteFlag))
                    .ThenInclude(b => b.BookmarkTags.Where(bt => !bt.DeleteFlag))
                    .ThenInclude(bt => bt.Tag)
                .AsEnumerable()
                .Select(d => new SearchResult
                {
                    Item = d,
                    Type = SearchItemType.Word,
                    Similarity = 90,
                    IsBookmarked = d.Bookmarks.Count != 0,
                    TagNames = d.Bookmarks
                        .Select(b => b.BookmarkTags)
                        .SelectMany(bt => bt)
                        .Select(bt => bt.Tag.TagName)
                });
            results.AddRange(startsWithDictionaries);
        }

        // Contains matches
        if (results.Count < maxResults)
        {
            var remainingSlots = maxResults - results.Count;
            var containsDictionaries = Context.Dictionaries
                .AsNoTracking()
                .WhereNotDeleted()
                .Where(d => d.Word.ToLower().Contains(keyword) && !d.Word.ToLower().StartsWith(keyword)
                            || d.Pronounce!.ToLower().Contains(keyword) && !d.Pronounce.ToLower().StartsWith(keyword))
                .Take(remainingSlots)
                .Include(d => d.Bookmarks.Where(b => !b.DeleteFlag))
                    .ThenInclude(b => b.BookmarkTags.Where(bt => !bt.DeleteFlag))
                    .ThenInclude(bt => bt.Tag)
                .AsEnumerable()
                .Select(d => new SearchResult
                {
                    Item = d,
                    Type = SearchItemType.Word,
                    Similarity = 70,
                    IsBookmarked = d.Bookmarks.Count != 0,
                    TagNames = d.Bookmarks
                        .Select(b => b.BookmarkTags)
                        .SelectMany(bt => bt)
                        .Select(bt => bt.Tag.TagName)
                });
            results.AddRange(containsDictionaries);
        }

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(maxResults);
    }

    private IEnumerable<SearchResult> TieredBookmarkedWordSearch(
        string keyword, int maxResults, int dbRetrieveLimit)
    {
        var results = new List<SearchResult>();

        // Exact matches
        var exactDictionaries = Context.Bookmarks
            .AsNoTracking()
            .WhereNotDeleted()
            .Where(b => b.Word.Word.ToLower() == keyword || b.Word.Pronounce!.ToLower() == keyword)
            .Take(dbRetrieveLimit)
            .Include(b => b.Word)
            .Include(b => b.BookmarkTags.Where(bt => !bt.DeleteFlag))
                .ThenInclude(bt => bt.Tag)
            .AsEnumerable()
            .Select(b => new SearchResult
            {
                Item = b.Word,
                Type = SearchItemType.Word,
                Similarity = 100,
                IsBookmarked = true,
                TagNames = b.BookmarkTags.Select(bt => bt.Tag.TagName)
            });
        results.AddRange(exactDictionaries);

        // Starts with matches
        if (results.Count < maxResults)
        {
            var remainingSlots = maxResults - results.Count;
            var startsWithDictionaries = Context.Bookmarks
                .AsNoTracking()
                .WhereNotDeleted()
                .Where(b => b.Word.Word.ToLower().StartsWith(keyword) && b.Word.Word.ToLower() != keyword
                            || b.Word.Pronounce!.ToLower().StartsWith(keyword) && b.Word.Pronounce!.ToLower() != keyword)
                .Take(remainingSlots)
                .Include(b => b.Word)
                .Include(b => b.BookmarkTags.Where(bt => !bt.DeleteFlag))
                    .ThenInclude(bt => bt.Tag)
                .AsEnumerable()
                .Select(b => new SearchResult
                {
                    Item = b.Word,
                    Type = SearchItemType.Word,
                    Similarity = 90,
                    IsBookmarked = true,
                    TagNames = b.BookmarkTags.Select(bt => bt.Tag.TagName)
                });
            results.AddRange(startsWithDictionaries);
        }

        // Contains matches
        if (results.Count < maxResults)
        {
            var remainingSlots = maxResults - results.Count;
            var containsDictionaries = Context.Bookmarks
                .AsNoTracking()
                .WhereNotDeleted()
                .Where(b => b.Word.Word.ToLower().Contains(keyword) && !b.Word.Word.ToLower().StartsWith(keyword)
                            || b.Word.Pronounce!.ToLower().Contains(keyword) && !b.Word.Pronounce.ToLower().StartsWith(keyword))
                .Take(remainingSlots)
                .Include(b => b.Word)
                .Include(b => b.BookmarkTags.Where(bt => !bt.DeleteFlag))
                    .ThenInclude(bt => bt.Tag)
                .AsEnumerable()
                .Select(b => new SearchResult
                {
                    Item = b.Word,
                    Type = SearchItemType.Word,
                    Similarity = 70,
                    IsBookmarked = true,
                    TagNames = b.BookmarkTags.Select(bt => bt.Tag.TagName)
                });
            results.AddRange(containsDictionaries);
        }

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(maxResults);
    }

    private IEnumerable<SearchResult> TieredPhraseSearch(
        string keyword, int maxResults, int dbRetrieveLimit)
    {
        var results = new List<SearchResult>();

        // Exact matches
        var exactPhrases = Context.Phrases
            .AsNoTracking()
            .WhereNotDeleted()
            .Where(p => p.PhraseText.ToLower() == keyword)
            .Take(dbRetrieveLimit)
            .AsEnumerable()
            .Select(p => new SearchResult
            {
                Item = p,
                Type = SearchItemType.Phrase,
                Similarity = 100,
                IsBookmarked = false
            });
        results.AddRange(exactPhrases);

        // Starts with matches
        if (results.Count < maxResults)
        {
            var remainingSlots = maxResults - results.Count;
            var startsWithPhrases = Context.Phrases
                .AsNoTracking()
                .WhereNotDeleted()
                .Where(p => p.PhraseText.ToLower().StartsWith(keyword)
                        && p.PhraseText.ToLower() != keyword)
                .Take(remainingSlots)
                .AsEnumerable()
                .Select(p => new SearchResult
                {
                    Item = p,
                    Type = SearchItemType.Phrase,
                    Similarity = 90,
                    IsBookmarked = false
                });
            results.AddRange(startsWithPhrases);
        }

        // Contains matches
        if (results.Count < maxResults)
        {
            var remainingSlots = maxResults - results.Count;
            var containsPhrases = Context.Phrases
                .AsNoTracking()
                .WhereNotDeleted()
                .Where(p => p.PhraseText.ToLower().Contains(keyword) &&
                           !p.PhraseText.ToLower().StartsWith(keyword))
                .Take(remainingSlots)
                .AsEnumerable()
                .Select(p => new SearchResult
                {
                    Item = p,
                    Type = SearchItemType.Phrase,
                    Similarity = 70,
                    IsBookmarked = false
                });
            results.AddRange(containsPhrases);
        }

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(maxResults);
    }

    private IEnumerable<SearchResult> StandardWordSearch(string keyword, int maxResults, int dbRetrieveLimit)
    {
        var dicQuery = Context.Dictionaries
            .AsNoTracking()
            .WhereNotDeleted();
        var results = StandardWordSearchBase(dicQuery, keyword, dbRetrieveLimit);

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(maxResults);
    }

    private static IEnumerable<SearchResult> StandardWordSearchBase(IQueryable<Dictionary> dictionaries,
        string keyword, int dbRetrieveLimit)
    {
        return dictionaries
            .Where(d => d.Word.ToLower().Contains(keyword)
                        || d.Pronounce!.ToLower().Contains(keyword))
            .Take(dbRetrieveLimit)
            .Include(d => d.Bookmarks.Where(b => !b.DeleteFlag))
                .ThenInclude(b => b.BookmarkTags.Where(bt => !bt.DeleteFlag))
                .ThenInclude(bt => bt.Tag)
            .AsEnumerable()
            .Select(d => new SearchResult
            {
                Item = d,
                Type = SearchItemType.Word,
                Similarity = CalculateSimilarity(keyword, d.Word),
                IsBookmarked = d.Bookmarks.Count != 0,
                TagNames = d.Bookmarks
                    .Select(b => b.BookmarkTags)
                    .SelectMany(bt => bt)
                    .Select(bt => bt.Tag.TagName)
            });
    }

    private IEnumerable<SearchResult> StandardBookmarkedWordSearch(string keyword, int maxResults, int dbRetrieveLimit)
    {
        var results = Context.Bookmarks
            .AsNoTracking()
            .WhereNotDeleted()
            .Where(b => b.Word.Word.ToLower().Contains(keyword)
                        || b.Word.Pronounce!.ToLower().Contains(keyword))
            .Take(dbRetrieveLimit)
            .Include(b => b.Word)
            .Include(b => b.BookmarkTags.Where(bt => !bt.DeleteFlag))
                .ThenInclude(bt => bt.Tag)
            .AsEnumerable()
            .Select(b => new SearchResult
            {
                Item = b.Word,
                Type = SearchItemType.Word,
                Similarity = CalculateSimilarity(keyword, b.Word.Word),
                IsBookmarked = true,
                TagNames = b.BookmarkTags.Select(bt => bt.Tag.TagName)
            });

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(maxResults);
    }

    private IEnumerable<SearchResult> StandardWordInTagSearch(string keyword, string filter,
        int maxResults, int dbRetrieveLimit)
    {
        keyword = keyword.ToLower();
        filter = filter.ToLower();

        var dicQuery = Context.Dictionaries
            .AsNoTracking()
            .WhereNotDeleted()
            .Where(d => d.Bookmarks
                .Where(b => !b.DeleteFlag)
                .Any(b => b.BookmarkTags
                    .Where(bt => !bt.DeleteFlag)
                    .Any(bt => bt.Tag.TagName.ToLower() == filter)));

        var results = StandardWordSearchBase(dicQuery, keyword, dbRetrieveLimit);

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(maxResults);
    }

    private IEnumerable<SearchResult> StandardWordInTagCatSearch(string keyword, string filter,
        int maxResults, int dbRetrieveLimit)
    {
        var dicQuery = Context.Dictionaries
            .AsNoTracking()
            .WhereNotDeleted()
            .Where(d => d.Bookmarks
                .Where(b => !b.DeleteFlag)
                .Any(b => b.BookmarkTags
                    .Where(bt => !bt.DeleteFlag)
                    .Any(bt => bt.Tag.TagCategory.CategoryName.ToLower() == filter)));
        var results = StandardWordSearchBase(dicQuery, keyword, dbRetrieveLimit);

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(maxResults);
    }

    private static IEnumerable<SearchResult> StandardPhraseSearchBase(IQueryable<Phrase> phrases,
        string keyword, int dbRetrieveLimit)
    {
        return phrases
            .Where(p => p.PhraseText.ToLower().Contains(keyword))
            .Take(dbRetrieveLimit)
            .Include(p => p.PhraseTags.Where(pt => !pt.DeleteFlag))
                .ThenInclude(pt => pt.Tag)
            .AsEnumerable()
            .Select(p => new SearchResult
            {
                Item = p,
                Type = SearchItemType.Phrase,
                Similarity = CalculateSimilarity(keyword, p.PhraseText),
                IsBookmarked = false,
                TagNames = p.PhraseTags.Select(pt => pt.Tag.TagName)
            });
    }

    private IEnumerable<SearchResult> StandardPhraseSearch(string keyword, int maxResults, int dbRetrieveLimit)
    {
        var phraseQuery = Context.Phrases
            .AsNoTracking()
            .WhereNotDeleted();
        var results = StandardPhraseSearchBase(phraseQuery, keyword, dbRetrieveLimit);

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(maxResults);
    }

    private IEnumerable<SearchResult> StandardPhraseInTagSearch(string keyword, string filter,
        int maxResults, int dbRetrieveLimit)
    {
        var queryQuery = Context.Phrases
            .AsNoTracking()
            .WhereNotDeleted()
            .Where(p => p.PhraseTags
                .Where(pt => !pt.DeleteFlag)
                .Any(pt => pt.Tag.TagName.ToLower() == filter));

        var results = StandardPhraseSearchBase(queryQuery, keyword, dbRetrieveLimit);

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(maxResults);
    }

    private IEnumerable<SearchResult> StandardPhraseInTagCatSearch(string keyword, string filter,
        int maxResults, int dbRetrieveLimit)
    {
        var queryQuery = Context.Phrases
            .AsNoTracking()
            .WhereNotDeleted()
            .Where(p => p.PhraseTags
                .Where(pt => !pt.DeleteFlag)
                .Any(pt => pt.Tag.TagCategory.CategoryName.ToLower() == filter));

        var results = StandardPhraseSearchBase(queryQuery, keyword, dbRetrieveLimit);

        return results
            .OrderByDescending(r => r.Similarity)
            .Take(maxResults);
    }

    private static double CalculateSimilarity(string source, string target)
    {
        if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
            return 0;

        source = source.ToLower();
        target = target.ToLower();

        if (target == source)
            return 100;

        if (target.StartsWith(source))
            return 90;

        if (target.Contains(source))
            return 70;

        var distance = source.LevenshteinDistance(target);
        var maxLength = Math.Max(source.Length, target.Length);
        var similarity = (1.0 - (double)distance / maxLength) * 50;

        return Math.Max(0, similarity);
    }
}
