using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.Shared.Helpers;
using BearWordsAPI.Shared.Services;
using BearWordsMaui.Services.DbServices.DataItems;
using Microsoft.EntityFrameworkCore;

namespace BearWordsMaui.Services.DbServices;

public class PhraseService
{
    private readonly ConfigService _config;
    private readonly IUUIDGenerator _uuid;
    private readonly IDbContextService _dbContextService;
    private const string ALL_LANG_STRING = "ALL";
    private BearWordsContext Context => _dbContextService.GetDbContext();

    public PhraseService(IDbContextService dbContextService, ConfigService config, IUUIDGenerator uuid)
    {
        _dbContextService = dbContextService;
        _config = config;
        _uuid = uuid;
    }

    public async Task<PaginatedResult<Phrase>> GetPhrasesAsync(
        string? languageFilter = null, SortOption sortOption = SortOption.Modified,
        int page = 1, int pageSize = 10)
    {
        var query = Context.Phrases
            .AsNoTracking()
            .WhereNotDeleted()
            .Include(p => p.PhraseLanguageNavigation)
            .Include(p => p.PhraseTags.Where(pt => !pt.DeleteFlag))
                .ThenInclude(pt => pt.Tag)
            .AsQueryable();

        // Apply language filter
        if (!string.IsNullOrEmpty(languageFilter) && languageFilter != ALL_LANG_STRING)
        {
            query = query.Where(p => p.PhraseLanguage == languageFilter);
        }

        // Apply sorting
        query = sortOption switch
        {
            SortOption.Modified => query.OrderByDescending(p => p.ModifiedAt),
            SortOption.ModifiedAsc => query.OrderBy(p => p.ModifiedAt),
            SortOption.Alphabetical => query.OrderBy(p => p.PhraseText),
            SortOption.AlphabeticalDesc => query.OrderByDescending(p => p.PhraseText),
            _ => query.OrderByDescending(p => p.ModifiedAt)
        };

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PaginatedResult<Phrase>
        {
            Items = items,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };
    }

    public async Task<Phrase?> GetPhraseByTextAsync(string phraseText)
    {
        return await Context.Phrases
            .AsNoTracking()
            .WhereNotDeleted()
            .FirstOrDefaultAsync(p => p.PhraseText == phraseText);
    }

    public async Task DeletePhraseAsync(string phraseId)
    {
        var phrase = await Context.Phrases
            .FirstOrDefaultAsync(p => p.PhraseId == phraseId);

        if (phrase is not null)
        {
            phrase.SetDeleteFlag();
            await Context.SaveChangesAsync(updateTimestamps: true, cascadeSoftDelete: true);
        }
    }

    public async Task<Phrase> CreatePhraseAsync(string phraseText, string languageCode, string? note = null)
    {
        var phrase = await Context.Phrases
            .FirstOrDefaultAsync(p => p.PhraseText == phraseText && p.PhraseLanguage == languageCode);

        if (phrase is null)
        {
            phrase = new Phrase
            {
                PhraseId = _uuid.Generate(),
                PhraseText = phraseText,
                PhraseLanguage = languageCode,
                Note = note,
                UserName = _config.UserName
            };

            Context.Phrases.Add(phrase);
        }
        else
        {
            if (!phrase.DeleteFlag) throw new Exception("The phrase already exists.");

            phrase.UnsetDeleteFlag();
            phrase.Note = note;
        }

        await Context.SaveChangesAsync(updateTimestamps: true, cascadeSoftDelete: true);

        return phrase;
    }

    public async Task<Phrase?> GetPhraseByIdAsync(string phraseId)
    {
        return await Context.Phrases
            .AsNoTracking()
            .WhereNotDeleted()
            .Include(p => p.PhraseLanguageNavigation)
            .Include(p => p.PhraseTags.Where(pt => !pt.DeleteFlag))
                .ThenInclude(pt => pt.Tag)
            .FirstOrDefaultAsync(p => p.PhraseId == phraseId);
    }

    public async Task UpdatePhraseAsync(string phraseId, string phraseText,
        string languageCode, string? note)
    {
        var phrase = await Context.Phrases
            .WhereNotDeleted()
            .FirstOrDefaultAsync(p => p.PhraseId == phraseId);

        if (phrase is null) return;

        phrase.PhraseText = phraseText;
        phrase.PhraseLanguage = languageCode;
        phrase.Note = note;

        await Context.SaveChangesAsync(updateTimestamps: true);
    }

    public async Task UpdatePhraseTagAsync(string phraseId, string[] tagIds)
    {
        var phrase = await Context.Phrases
            .WhereNotDeleted()
            .Include(p => p.PhraseTags)
            .FirstOrDefaultAsync(p => p.PhraseId == phraseId);

        foreach (var existingTag in phrase!.PhraseTags.WhereNotDeleted())
        {
            existingTag.SetDeleteFlag();
        }

        foreach (var tagId in tagIds)
        {
            var phraseTag = phrase!.PhraseTags.FirstOrDefault(pt => pt.TagId == tagId);

            if (phraseTag is null)
            {
                phraseTag = new PhraseTag
                {
                    PhraseTagId = _uuid.Generate(),
                    PhraseId = phraseId,
                    TagId = tagId
                };

                await Context.PhraseTags.AddAsync(phraseTag);
            }
            else
            {
                phraseTag.UnsetDeleteFlag();
            }
        }

        await Context.SaveChangesAsync(updateTimestamps: true, cascadeSoftDelete: true);
    }
}
