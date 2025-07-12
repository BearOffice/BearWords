using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.Shared.Helpers;
using BearWordsAPI.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace BearWordsMaui.Services.DbServices;

public class WordService
{
    private readonly ConfigService _config;
    private readonly IUUIDGenerator _uuid;
    private readonly IDbContextService _dbContextService;
    private BearWordsContext Context => _dbContextService.GetDbContext();

    public WordService(IDbContextService dbContextService, ConfigService config, IUUIDGenerator uuid)
    {
        _dbContextService = dbContextService;
        _config = config;
        _uuid = uuid;
    }

    public async Task<Dictionary?> GetWordAsync(int wordId)
    {
        var dictionary = await Context.Dictionaries.FindAsync(wordId);

        if (dictionary == null || dictionary.DeleteFlag)
            return null;

        Context.Entry(dictionary)
            .Collection(d => d.Translations)
            .Query()
            .Where(t => !t.DeleteFlag)
            .Include(t => t.TargetLanguageNavigation)
            .Load();

        return dictionary;
    }

    public async Task<Bookmark?> GetBookmarkAsync(int wordId)
    {
        return await Context.Bookmarks
            .AsNoTracking()
            .WhereNotDeleted()
            .Where(b => b.WordId == wordId)
            .Include(b => b.BookmarkTags.Where(bt => !bt.DeleteFlag))
                .ThenInclude(bt => bt.Tag)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> ToggleBookmarkAsync(int wordId)
    {
        var bookmark = await Context.Bookmarks
            .FirstOrDefaultAsync(b => b.WordId == wordId);

        if (bookmark is null)
        {
            bookmark = new Bookmark
            {
                BookmarkId = _uuid.Generate(),
                WordId = wordId,
                UserName = _config.UserName
            };
            await Context.Bookmarks.AddAsync(bookmark);
        }
        else if (bookmark.DeleteFlag)
        {
            bookmark.UnsetDeleteFlag();
        }
        else
        {
            bookmark.SetDeleteFlag();
        }

        await Context.SaveChangesAsync(updateTimestamps: true, cascadeSoftDelete: true);
        return !bookmark.DeleteFlag;
    }
}