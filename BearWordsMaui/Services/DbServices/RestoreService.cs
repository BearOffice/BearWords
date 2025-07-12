using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.Shared.Helpers;
using BearWordsMaui.Services;
using BearWordsMaui.Services.DbServices.DataItems;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BearWordsMaui.Services.DbServices;

public class RestoreService
{
    private readonly IDbContextService _dbContextService;
    private readonly ConfigService _config;
    private BearWordsContext Context => _dbContextService.GetDbContext();

    public RestoreService(IDbContextService dbContextService, ConfigService config)
    {
        _dbContextService = dbContextService;
        _config = config;
    }

    public async Task<PaginatedResult<DeletedItem>> GetDeletedItemsAsync(int page = 1, int pageSize = 20)
    {
        var hideBefore = _config.HideDeletedItemBefore;

        var phrasesQuery = Context.Phrases
            .WhereDeleted()
            .Where(p => p.ModifiedAt >= hideBefore)
            .Select(p => new DeletedItem
            {
                Id = p.PhraseId,
                Type = DeletedItemType.Phrase,
                Name = p.PhraseText,
                DeletedAt = p.ModifiedAt
            });

        var bookmarksQuery = Context.Bookmarks
            .WhereDeleted()
            .Where(b => b.ModifiedAt >= hideBefore)
            .Include(b => b.Word)
            .Select(b => new DeletedItem
            {
                Id = b.BookmarkId,
                Type = DeletedItemType.Bookmark,
                Name = b.Word.Word,
                DeletedAt = b.ModifiedAt
            });

        var tagCategoriesQuery = Context.TagCategories
            .WhereDeleted()
            .Where(tc => tc.ModifiedAt >= hideBefore)
            .Select(tc => new DeletedItem
            {
                Id = tc.TagCategoryId,
                Type = DeletedItemType.TagCategory,
                Name = tc.CategoryName,
                DeletedAt = tc.ModifiedAt
            });

        var tagsQuery = Context.Tags
            .WhereDeleted()
            .Where(t => t.ModifiedAt >= hideBefore)
            .Include(t => t.TagCategory)
            .Select(t => new DeletedItem
            {
                Id = t.TagId,
                Type = DeletedItemType.Tag,
                Name = t.TagName,
                DeletedAt = t.ModifiedAt
            });

        var combinedQuery = phrasesQuery
            .Union(bookmarksQuery)
            .Union(tagCategoriesQuery)
            .Union(tagsQuery)
            .OrderByDescending(item => item.DeletedAt);

        var totalItems = await combinedQuery.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = await combinedQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<DeletedItem>
        {
            Items = items,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };
    }

    public async Task<bool> RestoreItemAsync(string itemId, DeletedItemType itemType)
    {
        try
        {
            switch (itemType)
            {
                case DeletedItemType.Phrase:
                    var phrase = await Context.Phrases
                        .WhereDeleted()
                        .FirstOrDefaultAsync(p => p.PhraseId == itemId);
                    phrase!.UnsetDeleteFlag();
                    break;

                case DeletedItemType.Bookmark:
                    var bookmark = await Context.Bookmarks
                        .WhereDeleted()
                        .FirstOrDefaultAsync(b => b.BookmarkId == itemId);
                    bookmark!.UnsetDeleteFlag();
                    break;

                case DeletedItemType.TagCategory:
                    var tagCategory = await Context.TagCategories
                        .WhereDeleted()
                        .FirstOrDefaultAsync(tc => tc.TagCategoryId == itemId);
                    tagCategory!.UnsetDeleteFlag();
                    break;

                case DeletedItemType.Tag:
                    var tag = await Context.Tags
                        .WhereDeleted()
                        .FirstOrDefaultAsync(t => t.TagId == itemId);
                    tag!.UnsetDeleteFlag();
                    break;

                default:
                    return false;
            }

            await Context.SaveChangesAsync(updateTimestamps: true, cascadeSoftDelete: true);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // Cannot actually remove these items.
    // For example, if a bookmark is untoggled and removed after syncing,
    // then that same bookmark is toggled again,
    // the two bookmarks will have the same word id but different bookmark ids.
    // This will cause a unique constraint violation on the server side when attempting to sync the newly created data.
    public async Task HardRemoveAll()
    {
        var clientSync = Context.Syncs.FirstOrDefault(s => s.ClientId == _config.ClientId)!;
        var lastPushTime = clientSync.LastPush;

        var phrasesQuery = WhereDeletedBefore(Context.Phrases.AsQueryable(), lastPushTime);
        var bookmarksQuery = WhereDeletedBefore(Context.Bookmarks.AsQueryable(), lastPushTime);
        var tagCategoriesQuery = WhereDeletedBefore(Context.TagCategories.AsQueryable(), lastPushTime);
        var tagsQuery = WhereDeletedBefore(Context.Tags.AsQueryable(), lastPushTime);

        foreach (var phrase in phrasesQuery) 
        {
            phrase.Note = null;
        }

        foreach (var bookmark in bookmarksQuery)
        {
            bookmark.Note = null;
        }

        foreach (var tagCategory in tagCategoriesQuery)
        {
            tagCategory.Description = null;
        }

        foreach (var tag in tagsQuery)
        {
            tag.Description = null;
        }

        await Context.SaveChangesAsync();
        _config.HideDeletedItemBefore = lastPushTime;
    }

    private IQueryable<T> WhereDeletedBefore<T>(IQueryable<T> query, long datetimeTick)
        where T : ITimestamps, ISoftDeletable
    {
        return query
            .WhereDeleted()
            .Where(x => x.ModifiedAt < datetimeTick && x.ModifiedAt >= _config.HideDeletedItemBefore);
    }
}