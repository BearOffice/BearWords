using BearWordsAPI.Shared.Data.Models;
using BearWordsMaui.Services;
using Microsoft.EntityFrameworkCore;

namespace BearWordsMaui.Services.DbServices;

public class LanguageService
{
    private readonly IDbContextService _dbContextService;
    private BearWordsContext Context => _dbContextService.GetDbContext();

    public LanguageService(IDbContextService dbContextService)
    {
        _dbContextService = dbContextService;
    }

    public async Task<Language[]> GetLanguagesAsync()
    {
        return await Context.Languages
            .AsNoTracking()
            .OrderBy(l => l.LanguageCode)
            .ToArrayAsync();
    }
}
