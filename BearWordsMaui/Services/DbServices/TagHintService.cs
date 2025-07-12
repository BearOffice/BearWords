using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using BearWordsAPI.Shared.Helpers;
using System.Threading.Tasks;

namespace BearWordsMaui.Services.DbServices;

public class TagHintService
{
    private readonly IDbContextService _dbContextService;
    private readonly SpecialDataService _specialDataService;
    private BearWordsContext Context => _dbContextService.GetDbContext();

    public TagHintService(IDbContextService dbContextService, SpecialDataService specialDataService)
    {
        _dbContextService = dbContextService;
        _specialDataService = specialDataService;
    }

    public string[] GetTagHints(string key)
    {
        var tagHints = GetTagHintsData();

        key = key.ToLower();
        var tags = tagHints
            .Where(pair => pair.Value.Contains(key))
            .Select(pair => pair.Key)
            .ToList();

        return Context.Tags
            .AsNoTracking()
            .Where(t => tags.Contains(t.TagName))
            .Select(t => t.TagId)
            .ToArray();
    }

    private Dictionary<string, List<string>> GetTagHintsData()
    {
        var mergedData = new Dictionary<string, List<string>>();

        try
        {
            var rawData = _specialDataService.GetData(ConfigService.SP_TAG_HINT);

            foreach (var raw in rawData)
            {
                if (raw.Value is null) continue;
                var data = JsonSerializer.Deserialize<Dictionary<string, string[]>>(raw.Value);

                if (data is not null)
                {
                    foreach (var pair in data)
                    {
                        if (mergedData.TryGetValue(pair.Key, out var existingList))
                        {
                            existingList.AddRange(pair.Value);
                        }
                        else
                        {
                            mergedData[pair.Key] = [.. pair.Value];
                        }
                    }
                }
            }
        }
        catch { }

        return mergedData;
    }
}