using BearWordsMaui.Services.DbServices.DataItems;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace BearWordsMaui.Services.DbServices;

public class ExportService
{
    private readonly BookmarkService _bookmarkService;
    private readonly PhraseService _phraseService;
    private readonly TagService _tagService;

    private static readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public ExportService(BookmarkService bookmarkService, PhraseService phraseService, TagService tagService)
    {
        _bookmarkService = bookmarkService;
        _phraseService = phraseService;
        _tagService = tagService;
    }

    public async Task<ExportResult> ExportAllDataAsync()
    {
        var tagCategoryData = await ExportTagCategoriesAsync();
        var bookmarkData = await ExportBookmarksAndPhrasesAsync();

        var result = new ExportResult
        {
            TagCategoryJson = JsonSerializer.Serialize(tagCategoryData, _jsonOptions),
            BookmarkDataJson = JsonSerializer.Serialize(bookmarkData, _jsonOptions)
        };

        return result;
    }

    public async Task<ExportTagData[]> ExportTagCategoriesAsync()
    {
        var dataDic = new Dictionary<string, ExportTagData>();

        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            var tagCategoriesResult = await _tagService.GetTagCategoriesWithTagsAsync(
                sortOption: SortOption.Alphabetical, page: page, pageSize: pageSize);

            if (tagCategoriesResult.Items.Count == 0)
                break;

            foreach (var container in tagCategoriesResult.Items)
            {
                if (!dataDic.TryGetValue(container.CategoryName, out var data))
                {
                    dataDic[container.CategoryName] = new ExportTagData
                    {
                        Category = container.CategoryName,
                        Description = container.Description,
                        Tags = container.Tags
                            .Select(t => KeyValuePair.Create(t.TagName, t.Description))
                            .ToDictionary()
                    };
                }
                else
                {
                    foreach (var tag in container.Tags)
                    {
                        data.Tags.Add(tag.TagName, tag.Description);
                    }
                }
            }

            page++;
        }

        return [.. dataDic.Select(pair => pair.Value)];
    }

    public async Task<ExportWordData[]> ExportBookmarksAndPhrasesAsync()
    {
        var bookmarks = await ExportBookmarks();
        var phrases = await ExportPhrases();

        return [.. bookmarks, .. phrases];
    }

    public async Task<ExportWordData[]> ExportBookmarks()
    {
        var exportData = new List<ExportWordData>();

        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            var bookmarksResult = await _bookmarkService.GetBookmarksAsync(
                sortOption: SortOption.ModifiedAsc, page: page, pageSize: pageSize);

            if (bookmarksResult.Items.Count == 0)
                break;

            foreach (var bookmark in bookmarksResult.Items)
            {
                exportData.Add(new ExportWordData
                {
                    Lang = bookmark.Word.SourceLanguage,
                    Title = bookmark.Word.Word,
                    Alias = [],
                    Note = bookmark.Note,
                    Tags = bookmark.BookmarkTags.Select(bt => bt.Tag.TagName).ToArray()
                });
            }

            page++;
        }

        return [.. exportData];
    }

    public async Task<ExportWordData[]> ExportPhrases()
    {
        var exportData = new List<ExportWordData>();

        var page = 1;
        const int pageSize = 100;

        while (true)
        {
            var phrasesResult = await _phraseService.GetPhrasesAsync(
                sortOption: SortOption.ModifiedAsc, page: page, pageSize: pageSize);

            if (phrasesResult.Items.Count == 0)
                break;

            foreach (var phrase in phrasesResult.Items)
            {
                exportData.Add(new ExportWordData
                {
                    Lang = phrase.PhraseLanguage,
                    Title = phrase.PhraseText,
                    Alias = [],
                    Note = phrase.Note,
                    Tags = phrase.PhraseTags.Select(pt => pt.Tag.TagName).ToArray()
                });
            }

            page++;
        }

        return [.. exportData];
    }
}