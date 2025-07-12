using BearWordsAPI.Shared.Services;
using BearWordsMaui.Helpers;
using BearWordsMaui.Services.DbServices.DataItems;
using Microsoft.EntityFrameworkCore;

namespace BearWordsMaui.Services.DbServices;

public class ConflictService
{
    private readonly IDbContextService _dbContextService;

    private BearWordsContext Context => _dbContextService.GetDbContext();

    public ConflictService(IDbContextService dbContextService)
    {
        _dbContextService = dbContextService;
    }

    public async Task<PaginatedResult<ConflictContainer>> GetConflictsAsync(
        string? targetId = null,
        int page = 1,
        int pageSize = 30)
    {
        var query = Context.ConflictLogs.AsNoTracking();

        if (!string.IsNullOrEmpty(targetId))
        {
            query = query.Where(c => c.TargetId == targetId);
        }

        query = query.OrderByDescending(c => c.ReportedAt);

        var totalItems = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

        var conflictLogs = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var containers = new List<ConflictContainer>();

        foreach (var log in conflictLogs)
        {
            var container = new ConflictContainer
            {
                ConflictLogId = log.ConflictLogId,
                TargetId = log.TargetId,
                Detail = log.Detail,
                ReportedAt = log.ReportedAt
            };

            // Determine target type and display name
            await PopulateTargetInfo(container);
            containers.Add(container);
        }

        return new PaginatedResult<ConflictContainer>
        {
            Items = containers,
            CurrentPage = page,
            TotalPages = totalPages,
            TotalItems = totalItems,
            PageSize = pageSize
        };
    }

    private async Task PopulateTargetInfo(ConflictContainer container)
    {
        var targetId = container.TargetId;

        // Check TagCategory
        var tagCategory = await Context.TagCategories.FindAsync(targetId);
        if (tagCategory is not null)
        {
            container.TargetType = ConflictTargetType.TagCategory;
            container.TargetDisplayName = tagCategory.CategoryName;
            return;
        }

        // Check Tag
        var tag = await Context.Tags.FindAsync(targetId);
        if (tag is not null)
        {
            container.TargetType = ConflictTargetType.Tag;
            container.TargetDisplayName = tag.TagName;
            return;
        }

        // Check Phrase
        var phrase = await Context.Phrases.FindAsync(targetId);
        if (phrase is not null)
        {
            container.TargetType = ConflictTargetType.Phrase;
            container.TargetDisplayName = phrase.PhraseText;
            return;
        }

        // Check PhraseTag
        var phraseTag = await Context.PhraseTags.FindAsync(targetId); ;
        if (phraseTag is not null)
        {
            await Context.Entry(phraseTag)
                .Reference(pt => pt.Phrase)
                .LoadAsync();

            await Context.Entry(phraseTag)
                .Reference(pt => pt.Tag)
                .LoadAsync();

            container.TargetType = ConflictTargetType.PhraseTag;
            var phraseText = phraseTag.Phrase?.PhraseText ?? "Unknown Phrase";
            var tagName = phraseTag.Tag?.TagName ?? "Unknown Tag";
            container.TargetDisplayName = $"{phraseText.TruncateSmart(30)} - {tagName}";
            return;
        }

        // Check Bookmark
        var bookmark = await Context.Bookmarks.FindAsync(targetId);
        if (bookmark is not null)
        {
            await Context.Entry(bookmark)
                .Reference(b => b.Word)
                .LoadAsync();

            container.TargetType = ConflictTargetType.Bookmark;
            container.TargetDisplayName = bookmark.Word?.Word ?? "Unknown Word";
            return;
        }

        // Check BookmarkTag
        var bookmarkTag = await Context.BookmarkTags.FindAsync(targetId); ;
        if (bookmarkTag is not null)
        {
            await Context.Entry(bookmarkTag)
                .Reference(bt => bt.Bookmark)
                .LoadAsync();

            await Context.Entry(bookmarkTag.Bookmark)
                .Reference(b => b.Word)
                .LoadAsync();

            await Context.Entry(bookmarkTag)
                .Reference(bt => bt.Tag)
                .LoadAsync();

            container.TargetType = ConflictTargetType.BookmarkTag;
            var word = bookmarkTag.Bookmark?.Word?.Word ?? "Unknown Word";
            var tagName = bookmarkTag.Tag?.TagName ?? "Unknown Tag";
            container.TargetDisplayName = $"{word} - {tagName}";
            return;
        }

        // Target not found or deleted
        container.TargetType = ConflictTargetType.Unknown;
        container.TargetDisplayName = "Deleted or Unknown Target";
    }
}