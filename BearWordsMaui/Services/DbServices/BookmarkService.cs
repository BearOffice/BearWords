using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.Shared.Helpers;
using BearWordsAPI.Shared.Services;
using BearWordsMaui.Services.DbServices.DataItems;
using Microsoft.EntityFrameworkCore;

namespace BearWordsMaui.Services.DbServices;

public class BookmarkService
{
    private readonly IUUIDGenerator _uuid;
    private readonly IDbContextService _dbContextService;
    private readonly TagHintService _tagHintService;
    private const string ALL_LANG_STRING = "ALL";
    private BearWordsContext Context => _dbContextService.GetDbContext();

    public BookmarkService(IDbContextService dbContextService, IUUIDGenerator uuid, TagHintService tagHintService)
    {
        _dbContextService = dbContextService;
        _uuid = uuid;
        _tagHintService = tagHintService;
    }

    public async Task<PaginatedResult<Bookmark>> GetBookmarksAsync(string? languageFilter = null,
        SortOption sortOption = SortOption.Modified, int page = 1, int pageSize = 20)
    {
        var query = CreateBookmarksQuery();
        query = FilterLanguage(query, languageFilter);
        query = SortBy(query, sortOption);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<Bookmark>
        {
            Items = items,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };
    }

    private IQueryable<Bookmark> CreateBookmarksQuery()
    {
        return Context.Bookmarks
            .AsNoTracking()
            .WhereNotDeleted()
            .Include(b => b.Word)
                .ThenInclude(w => w.SourceLanguageNavigation)
            .Include(b => b.BookmarkTags.Where(bt => !bt.DeleteFlag))
                .ThenInclude(bt => bt.Tag)
            .AsQueryable();
    }

    private static IQueryable<Bookmark> FilterLanguage(IQueryable<Bookmark> query, string? langFilter)
    {
        if (!string.IsNullOrEmpty(langFilter) && langFilter != ALL_LANG_STRING)
        {
            return query.Where(b => b.Word.SourceLanguage == langFilter);
        }
        return query;
    }

    private static IQueryable<Bookmark> SortBy(IQueryable<Bookmark> query, SortOption sortOption)
    {
        return sortOption switch
        {
            SortOption.Modified => query.OrderByDescending(b => b.ModifiedAt),
            SortOption.ModifiedAsc => query.OrderBy(b => b.ModifiedAt),
            SortOption.Alphabetical => query.OrderBy(b => b.Word.Word),
            SortOption.AlphabeticalDesc => query.OrderByDescending(b => b.Word.Word),
            _ => query.OrderByDescending(b => b.ModifiedAt)
        };
    }

    public async Task UnBookmarkAsync(string bookmarkId)
    {
        var bookmark = await Context.Bookmarks
            .WhereNotDeleted()
            .FirstOrDefaultAsync(b => b.BookmarkId == bookmarkId);

        if (bookmark is not null)
        {
            bookmark.SetDeleteFlag();
            await Context.SaveChangesAsync(updateTimestamps: true, cascadeSoftDelete: true);
        }
    }

    public async Task<Bookmark?> GetBookmarkByIdAsync(string bookmarkId)
    {
        return await Context.Bookmarks
            .AsNoTracking()
            .WhereNotDeleted()
            .Include(b => b.Word)
                .ThenInclude(w => w.SourceLanguageNavigation)
            .Include(b => b.BookmarkTags.Where(bt => !bt.DeleteFlag))
                .ThenInclude(bt => bt.Tag)
                    .ThenInclude(t => t.TagCategory)
            .FirstOrDefaultAsync(b => b.BookmarkId == bookmarkId);
    }

    public async Task UpdateBookmarkAsync(string bookmarkId, string? note)
    {
        var bookmark = await Context.Bookmarks
            .WhereNotDeleted()
            .FirstOrDefaultAsync(b => b.BookmarkId == bookmarkId);

        if (bookmark is not null)
        {
            bookmark.Note = note;
            await Context.SaveChangesAsync(updateTimestamps: true);
        }
    }

    public async Task UpdateBookmarkTagsAsync(string bookmarkId, string[] tagIds)
    {
        var bookmark = await Context.Bookmarks
            .WhereNotDeleted()
            .Include(b => b.BookmarkTags)
            .FirstOrDefaultAsync(b => b.BookmarkId == bookmarkId);

        foreach (var existingTag in bookmark!.BookmarkTags.WhereNotDeleted())
        {
            existingTag.SetDeleteFlag();
        }

        foreach (var tagId in tagIds)
        {
            var bookmarkTag = bookmark!.BookmarkTags.FirstOrDefault(bt => bt.TagId == tagId);

            if (bookmarkTag is null)
            {
                bookmarkTag = new BookmarkTag
                {
                    BookmarkTagId = _uuid.Generate(),
                    BookmarkId = bookmarkId,
                    TagId = tagId
                };

                await Context.BookmarkTags.AddAsync(bookmarkTag);
            }
            else
            {
                bookmarkTag.UnsetDeleteFlag();
            }
        }

        await Context.SaveChangesAsync(updateTimestamps: true, cascadeSoftDelete: true);
    }

    public async Task AddTagHintsIfEmptyAsync(string bookmarkId)
    {
        var bookmark = await GetBookmarkByIdAsync(bookmarkId);
        if (bookmark is null || bookmark.BookmarkTags.Count != 0) return;

        var hints = _tagHintService.GetTagHints(bookmark.Word.Word);
        await UpdateBookmarkTagsAsync(bookmarkId, hints);
    }
}