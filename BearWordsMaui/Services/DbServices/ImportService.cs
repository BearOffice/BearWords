using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.Shared.Helpers;
using BearWordsMaui.Services.DbServices.DataItems;
using BearWordsMaui.Services.Helpers;
using Microsoft.EntityFrameworkCore;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace BearWordsMaui.Services.DbServices;

public class ImportService
{
    private readonly IDbContextService _dbContextService;
    private readonly LanguageService _languageService;
    private readonly WordService _wordService;
    private readonly BookmarkService _bookmarkService;
    private readonly PhraseService _phraseService;
    private readonly TagHintService _tagHintService;
    private readonly TagService _tagService;
    private readonly ISyncExecService _syncExecService;
    private readonly ConfigService _config;

    private BearWordsContext Context => _dbContextService.GetDbContext();

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public ImportService(IDbContextService dbContextService, LanguageService languageService,
        WordService wordService, BookmarkService bookmarkService, PhraseService phraseService,
        TagHintService tagHintService, TagService tagService, ISyncExecService syncExecService,
        ConfigService config)
    {
        _dbContextService = dbContextService;
        _languageService = languageService;
        _wordService = wordService;
        _bookmarkService = bookmarkService;
        _phraseService = phraseService;
        _tagHintService = tagHintService;
        _tagService = tagService;
        _syncExecService = syncExecService;
        _config = config;
    }

    public async Task<(ImportSummary summary, string remainingData)> ImportWordsAsync(
        string jsonText, bool autoAddToPhrase, bool tagAutoSuggest)
    {
        var summary = new ImportSummary();
        var unprocessedItems = new List<ImportWordData>();

        ImportWordData[] importItems;
        try
        {
            importItems = JsonSerializer.Deserialize<ImportWordData[]>(jsonText, _jsonOptions)
                ?? throw new Exception("Invalid JSON format");
        }
        catch (Exception ex)
        {
            summary.Results.Add(new ImportResult
            {
                Title = "JSON Parse Error",
                Success = false,
                FailedReason = ex.Message
            });
            summary.FailedCount++;

            return (summary, jsonText);
        }

        // Block sync temporarily for preventing SyncAction.OnAction behavior
        _syncExecService.IgnoreExecRequests();

        var langs = await _languageService.GetLanguagesAsync();
        var langCodes = langs.Select(l => l.LanguageCode).ToArray();

        summary.TotalProcessed = importItems.Length;

        foreach (var item in importItems)
        {
            var result = await ProcessImportItem(item, autoAddToPhrase, tagAutoSuggest, langCodes);
            summary.Results.Add(result);

            if (result.Success)
            {
                summary.SuccessCount++;
            }
            else
            {
                summary.FailedCount++;
                unprocessedItems.Add(item);
            }
        }

        var remainingData = unprocessedItems.Count != 0
            ? JsonSerializer.Serialize(unprocessedItems, _jsonOptions) : string.Empty;

        _syncExecService.AcceptExecRequests();
        if (_config.SyncAction == SyncAction.OnAction)
            await _syncExecService.ExecuteAsync();

        return (summary, remainingData);
    }

    private async Task<ImportResult> ProcessImportItem(ImportWordData item, bool autoAddToPhrase,
        bool tagAutoSuggest, string[] validLangs)
    {
        var result = new ImportResult { Title = item.Title };

        try
        {
            // Check language
            if (!validLangs.Contains(item.Lang))
            {
                result.FailedReason = $"Language '{item.Lang}' not found";
                return result;
            }

            // Check if word is exists or auto create is enabled
            var word = await FindWordByTitleOrAlias(item.Title, item.Alias);
            if (word is null && !autoAddToPhrase)
            {
                result.FailedReason = "Word not found and auto create phrase is disabled";
                return result;
            }

            // Check bookmark and tags
            if (word is not null)
            {
                var existingBookmark = await _wordService.GetBookmarkAsync(word.WordId);
                if (existingBookmark is not null)
                {
                    result.FailedReason = "Word is already bookmarked";
                    return result;
                }

                var missingTags = await MissingTagsInDbAsync(item.Tags);
                if (missingTags.Length != 0)
                {
                    result.FailedReason = $"Tags not found: {string.Join(", ", missingTags)}";
                    return result;
                }
            }

            var givenTagIds = await GetTagIdsByNamesAsync(item.Tags);
            var tagIds = tagAutoSuggest ? AppendHintedTagIds(givenTagIds, item.Title) : givenTagIds;

            if (word is not null)
            {
                await _wordService.ToggleBookmarkAsync(word.WordId);

                var bookmark = await _wordService.GetBookmarkAsync(word.WordId);
                await _bookmarkService.UpdateBookmarkAsync(bookmark!.BookmarkId, item.Note);
                await _bookmarkService.UpdateBookmarkTagsAsync(bookmark!.BookmarkId, tagIds);

                result.ItemType = ImportItemType.Word;
            }
            else
            {
                // Check if phrase already exists
                var existingPhrase = await _phraseService.GetPhraseByTextAsync(item.Title);
                if (existingPhrase?.PhraseLanguage == item.Lang)
                {
                    result.FailedReason = "Phrase already exists";
                    return result;
                }

                var phrase = await _phraseService.CreatePhraseAsync(item.Title, item.Lang, item.Note);
                await _phraseService.UpdatePhraseTagAsync(phrase.PhraseId, tagIds);

                result.ItemType = ImportItemType.Phrase;
            }

            result.Success = true;
        }
        catch (Exception ex)
        {
            result.FailedReason = ex.Message;
        }

        return result;
    }

    private async Task<Dictionary?> FindWordByTitleOrAlias(string title, string[] aliases)
    {
        var word = await Context.Dictionaries
            .FirstOrDefaultAsync(w => w.Word == title || w.Pronounce == title);

        if (word is not null) return word;

        foreach (var alias in aliases)
        {
            if (string.IsNullOrWhiteSpace(alias)) continue;

            word = await Context.Dictionaries
                .FirstOrDefaultAsync(w => w.Word == alias || w.Pronounce == alias);

            if (word is not null) return word;
        }

        return null;
    }

    private async Task<string[]> MissingTagsInDbAsync(string[] tagNames)
    {
        if (tagNames.Length == 0) return [];

        var existingTagNames = await Context.Tags
            .AsNoTracking()
            .WhereNotDeleted()
            .Where(t => tagNames.Contains(t.TagName))
            .Select(t => t.TagName)
            .ToArrayAsync();

        var missing = tagNames.Where(tagName => !existingTagNames.Contains(tagName));
        return [.. missing];
    }

    private async Task<string[]> GetTagIdsByNamesAsync(string[] tagNames)
    {
        if (tagNames.Length == 0) return [];

        var tagIds = await Context.Tags
            .AsNoTracking()
            .WhereNotDeleted()
            .Where(t => tagNames.Contains(t.TagName))
            .Select(t => t.TagId)
            .ToArrayAsync();

        return tagIds;
    }

    private string[] AppendHintedTagIds(string[] tagIds, string keyword)
    {
        var hints = _tagHintService.GetTagHints(keyword);
        return [.. tagIds.Union(hints)];
    }

    public async Task<(ImportSummary summary, string remainingData)> ImportTagsAsync(string jsonText)
    {
        var summary = new ImportSummary();
        var unprocessedItems = new List<ImportTagData>();

        ImportTagData[] importItems;
        try
        {
            importItems = JsonSerializer.Deserialize<ImportTagData[]>(jsonText, _jsonOptions)
                ?? throw new Exception("Invalid JSON format");
        }
        catch (Exception ex)
        {
            summary.Results.Add(new ImportResult
            {
                Title = "JSON Parse Error",
                Success = false,
                FailedReason = ex.Message
            });
            summary.FailedCount++;

            return (summary, jsonText);
        }

        // Block sync temporarily for preventing SyncAction.OnAction behavior
        _syncExecService.IgnoreExecRequests();

        summary.TotalProcessed = importItems.Length;

        foreach (var item in importItems)
        {
            var result = await ProcessTagImport(item);
            summary.Results.Add(result);

            if (result.Success)
            {
                summary.SuccessCount++;
            }
            else
            {
                summary.FailedCount++;
                unprocessedItems.Add(item);
            }
        }

        var remainingData = unprocessedItems.Count != 0
            ? JsonSerializer.Serialize(unprocessedItems, _jsonOptions) : string.Empty;

        _syncExecService.AcceptExecRequests();
        if (_config.SyncAction == SyncAction.OnAction)
            await _syncExecService.ExecuteAsync();

        return (summary, remainingData);
    }

    private async Task<ImportResult> ProcessTagImport(ImportTagData item)
    {
        var result = new ImportResult { Title = item.Category, ItemType = ImportItemType.Tag };

        try
        {
            var tagCat = await _tagService.GetTagCategoryByNameAsync(item.Category);
            tagCat ??= await _tagService.CreateTagCategoryAsync(item.Category, item.Description);

            var successfulTags = new List<string>();
            var failedTags = new List<string>();

            foreach ((var name, var desc) in item.Tags)
            {
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                try
                {
                    var tag = await _tagService.GetTagByNameAsync(name);
                    tag ??= await _tagService.CreateTagAsync(name, desc, tagCat!.TagCategoryId);

                    successfulTags.Add(name);
                }
                catch (Exception)
                {
                    failedTags.Add(name);
                }
            }

            if (failedTags.Count != 0)
                result.FailedReason = $"Failed to create tags: {string.Join(", ", failedTags)}";
            else
                result.Success = true;
        }
        catch (Exception ex)
        {
            result.FailedReason = ex.Message;
        }

        return result;
    }
}