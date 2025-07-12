using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using BearWordsAPI.Shared.Helpers;
using System.Threading.Tasks;
using BearWordsMaui.Services.DbServices.DataItems;

namespace BearWordsMaui.Services.DbServices;

public class SpecialDataService
{
    private readonly ConfigService _config;
    private readonly IDbContextService _dbContextService;
    private BearWordsContext Context => _dbContextService.GetDbContext();

    public SpecialDataService(IDbContextService dbContextService, ConfigService config)
    {
        _dbContextService = dbContextService;
        _config = config;
    }

    public SpecialDataItem[] GetData(string spType)
    {
        if (!_config.SpDataDictionary.TryGetValue(spType, out var spDataInd))
        {
            return [];
        }

        var hintPhrases = Context.TagCategories
            .AsNoTracking()
            .WhereNotDeleted()
            .Where(tc => tc.CategoryName == _config.SpDataIndicator)
            .SelectMany(tc => tc.Tags)
            .Where(t => !t.DeleteFlag && t.TagName == spDataInd)
            .SelectMany(t => t.PhraseTags)
            .Where(pt => !pt.DeleteFlag)
            .Select(pt => pt.Phrase)
            .Where(p => !p.DeleteFlag && p.PhraseLanguage == "@none")
            .ToArray();

        return hintPhrases
            .Select(p => new SpecialDataItem { Key = p.PhraseText, Value = p.Note })
            .ToArray();
    }
}