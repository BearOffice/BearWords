using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.Shared.Helpers;
using BearWordsAPI.Shared.Services;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace BearWordsMaui.Services.DbServices;

public class BasicInfoService
{
    private readonly IDbContextService _dbContextService;
    private BearWordsContext Context => _dbContextService.GetDbContext();

    public BasicInfoService(IDbContextService dbContextService)
    {
        _dbContextService = dbContextService;
    }

    public async Task<int> GetBookmarkCountAsync()
    {
        return await Context.Bookmarks.AsNoTracking().CountAsync();
    }

    public async Task<int> GetTagCategoryCountAsync()
    {
        return await Context.TagCategories.AsNoTracking().CountAsync();
    }

    public async Task<int> GetTagCountAsync()
    {
        return await Context.Tags.AsNoTracking().CountAsync();
    }

    public async Task<int> GetPhraseCountAsync()
    {
        return await Context.Phrases.AsNoTracking().CountAsync();
    }

    public async Task<int> GetWordCountAsync()
    {
        return await Context.Dictionaries.AsNoTracking().CountAsync();
    }
}