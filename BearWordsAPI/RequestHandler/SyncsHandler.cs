using BearWordsAPI.Shared.Data;
using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.Shared.DTOs;
using BearWordsAPI.Shared.Helpers;
using BearWordsAPI.Shared.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BearWordsAPI.RequestHandler;

public record SyncPullRequest(string LastPullTime);
public record SyncPullResponse(SyncPullDto PullDto);
public record SyncPushRequest(SyncPushDto PushDto, string[] Overwrites);
public record SyncPushResponse(Dictionary<string, string> Failures);
public record SyncServerTimeResponse(string Time);

public static class SyncsHandler
{
    private const string _unauthorizedExceptionMessage = "Unauthorized operation.";
    private const string _notAppliedMessage = "Update not applied because the server already contains a newer version.";

    public static async Task<Ok<SyncPullResponse>> PostSyncPull
        ([FromRoute] string clientId, [FromBody] SyncPullRequest pullRequest,
            ClaimsPrincipal user, BearWordsContext db, IDateTimeService dateTimeService)
    {
        var serverPullStartTime = dateTimeService.GetCurrentTicksLong();

        var userName = user.Identity!.Name!;
        var sync = await db.Users
            .WhereUser(userName)
            .SelectMany(u => u.Syncs)
            .FirstOrDefaultAsync(s => s.ClientId == clientId);

        var clientLastPullTime = pullRequest.LastPullTime.ToDateTime().ToLong();
        var serverLastPullTime = sync!.LastPull;
        var lastPullTime = serverLastPullTime <= clientLastPullTime ? serverLastPullTime : clientLastPullTime;


        var languageTask = db.Languages
            .AsNoTracking()
            .Select(item => new LanguageDto(item))
            .ToArrayAsync();

        var dictionaryTask = db.Dictionaries
            .AsNoTracking()
            .Where(item => item.ModifiedAt > lastPullTime)
            .Select(item => new DictionaryDto(item))
            .ToArrayAsync();

        var translationTask = db.Translations
            .AsNoTracking()
            .Where(item => item.ModifiedAt > lastPullTime)
            .Select(item => new TranslationDto(item))
            .ToArrayAsync();

        var phraseTask = db.Phrases
            .AsNoTracking()
            .WhereUser(userName)
            .Where(item => item.ModifiedAt > lastPullTime)
            .Select(item => new PhraseDto(item))
            .ToArrayAsync();

        var phraseTagTask = db.PhraseTags
            .AsNoTracking()
            .WhereUser(userName)
            .Where(item => item.ModifiedAt > lastPullTime)
            .Select(item => new PhraseTagDto(item))
            .ToArrayAsync();

        var bookmarkTask = db.Bookmarks
            .AsNoTracking()
            .WhereUser(userName)
            .Where(item => item.ModifiedAt > lastPullTime)
            .Select(item => new BookmarkDto(item))
            .ToArrayAsync();

        var bookmarkTagTask = db.BookmarkTags
            .AsNoTracking()
            .WhereUser(userName)
            .Where(item => item.ModifiedAt > lastPullTime)
            .Select(item => new BookmarkTagDto(item))
            .ToArrayAsync();

        var tagCategoryTask = db.TagCategories
            .AsNoTracking()
            .WhereUser(userName)
            .Where(item => item.ModifiedAt > lastPullTime)
            .Select(item => new TagCategoryDto(item))
            .ToArrayAsync();

        var tagTask = db.Tags
            .AsNoTracking()
            .WhereUser(userName)
            .Where(item => item.ModifiedAt > lastPullTime)
            .Select(item => new TagDto(item))
            .ToArrayAsync();

        await Task.WhenAll(languageTask, phraseTask, phraseTagTask, dictionaryTask, translationTask,
            bookmarkTask, bookmarkTagTask, tagCategoryTask, tagTask);

        var syncDto = new SyncPullDto
        {
            Languages = languageTask.Result,
            Dictionaries = dictionaryTask.Result,
            Translations = translationTask.Result,
            Phrases = phraseTask.Result,
            PhraseTags = phraseTagTask.Result,
            Bookmarks = bookmarkTask.Result,
            BookmarkTags = bookmarkTagTask.Result,
            TagCategories = tagCategoryTask.Result,
            Tags = tagTask.Result,
        };

        sync.LastPull = serverPullStartTime;
        await db.SaveChangesAsync();

        return TypedResults.Ok(new SyncPullResponse(syncDto));
    }

    public static async Task<Results<Ok<SyncPushResponse>, NotFound<string>, BadRequest<SyncPushResponse>>>
        PostSyncPush([FromRoute] string clientId, [FromBody] SyncPushRequest pushRequest,
            ClaimsPrincipal user, BearWordsContext db, IDateTimeService dateTimeService, IUUIDGenerator uuid)
    {
        var serverPushStartTime = dateTimeService.GetCurrentTicksLong();

        var userName = user.Identity!.Name!;
        var sync = await db.Users
            .WhereUser(userName)
            .SelectMany(u => u.Syncs)
            .FirstOrDefaultAsync(s => s.ClientId == clientId);

        var failures = new Dictionary<string, string>();

        foreach (var obj in pushRequest.PushDto.TagCategories)
        {
            if (obj.UserName != userName)
            {
                failures.Add(obj.TagCategoryId, _unauthorizedExceptionMessage);
                continue;
            }

            var data = await db.TagCategories.FindAsync(obj.TagCategoryId);
            if (data is null)
            {
                await db.TagCategories.AddAsync(obj.ToEntity());
            }
            else if (obj.ModifiedAt > data.ModifiedAt)
            {
                // Overwrites check -> if contained, add conflict log
                if (pushRequest.Overwrites.Contains(data.TagCategoryId))
                {
                    await db.ConflictLogs.AddAsync(new ConflictLog
                    {
                        ConflictLogId = uuid.Generate(),
                        UserName = userName,
                        ClientId = clientId,
                        TargetId = data.TagCategoryId,
                        Detail = data.ReadableSerialize(clientId),
                        ReportedAt = serverPushStartTime,
                    });
                }

                data.CategoryName = obj.CategoryName;
                data.Description = obj.Description;
                data.ModifiedAt = obj.ModifiedAt;
                data.DeleteFlag = obj.DeleteFlag;
            }
            else
            {
                failures.Add(obj.TagCategoryId, _notAppliedMessage);
            }
        }

        foreach (var obj in pushRequest.PushDto.Tags)
        {
            var tagCat = await db.TagCategories.FindAsync(obj.TagCategoryId);
            if (tagCat is null)
            {
                failures.Add(obj.TagId, "The `TagCategoryId` does not exist.");
                continue;
            }
            if (tagCat.UserName != userName)
            {
                failures.Add(obj.TagId, _unauthorizedExceptionMessage);
                continue;
            }

            var data = await db.Tags.FindAsync(obj.TagId);
            if (data is null)
            {
                await db.Tags.AddAsync(obj.ToEntity());
            }
            else if (obj.ModifiedAt > data.ModifiedAt)
            {
                if (pushRequest.Overwrites.Contains(data.TagId))
                {
                    await db.ConflictLogs.AddAsync(new ConflictLog
                    {
                        ConflictLogId = uuid.Generate(),
                        UserName = userName,
                        ClientId = clientId,
                        TargetId = data.TagId,
                        Detail = data.ReadableSerialize(clientId),
                        ReportedAt = serverPushStartTime,
                    });
                }

                data.TagName = obj.TagName;
                data.TagCategoryId = obj.TagCategoryId;
                data.Description = obj.Description;
                data.ModifiedAt = obj.ModifiedAt;
                data.DeleteFlag = obj.DeleteFlag;
            }
            else
            {
                failures.Add(obj.TagId, _notAppliedMessage);
            }
        }

        foreach (var obj in pushRequest.PushDto.Phrases)
        {
            if (obj.UserName != userName)
            {
                failures.Add(obj.PhraseId, _unauthorizedExceptionMessage);
                continue;
            }

            var data = await db.Phrases.FindAsync(obj.PhraseId);
            if (data is null)
            {
                await db.Phrases.AddAsync(obj.ToEntity());
            }
            else if (obj.ModifiedAt > data.ModifiedAt)
            {
                if (pushRequest.Overwrites.Contains(data.PhraseId))
                {
                    await db.ConflictLogs.AddAsync(new ConflictLog
                    {
                        ConflictLogId = uuid.Generate(),
                        UserName = userName,
                        ClientId = clientId,
                        TargetId = data.PhraseId,
                        Detail = data.ReadableSerialize(clientId),
                        ReportedAt = serverPushStartTime,
                    });
                }

                data.PhraseText = obj.PhraseText;
                data.PhraseLanguage = obj.PhraseLanguage;
                data.Note = obj.Note;
                data.ModifiedAt = obj.ModifiedAt;
                data.DeleteFlag = obj.DeleteFlag;
            }
            else
            {
                failures.Add(obj.PhraseId, _notAppliedMessage);
            }
        }

        foreach (var obj in pushRequest.PushDto.PhraseTags)
        {
            var tag = await db.Tags.FindAsync(obj.TagId);
            var phrase = await db.Phrases.FindAsync(obj.PhraseId);
            if (tag is null)
            {
                failures.Add(obj.PhraseTagId, "The `TagId` does not exist.");
                continue;
            }
            if (phrase is null)
            {
                failures.Add(obj.PhraseTagId, "The `PhraseId` does not exist.");
                continue;
            }

            var tagCat = await db.TagCategories.FindAsync(tag.TagCategoryId);
            if (tagCat is null)
            {
                failures.Add(obj.PhraseTagId, "The `TagCategoryId` does not exist.");
                continue;
            }

            if (tagCat.UserName != userName || phrase.UserName != userName)
            {
                failures.Add(obj.PhraseTagId, _unauthorizedExceptionMessage);
                continue;
            }

            var data = await db.PhraseTags.FindAsync(obj.PhraseTagId);
            if (data is null)
            {
                await db.PhraseTags.AddAsync(obj.ToEntity());
            }
            else if (obj.ModifiedAt > data.ModifiedAt)
            {
                if (pushRequest.Overwrites.Contains(data.PhraseTagId))
                {
                    await db.ConflictLogs.AddAsync(new ConflictLog
                    {
                        ConflictLogId = uuid.Generate(),
                        UserName = userName,
                        ClientId = clientId,
                        TargetId = data.PhraseTagId,
                        Detail = data.ReadableSerialize(clientId),
                        ReportedAt = serverPushStartTime,
                    });
                }

                data.PhraseId = obj.PhraseId;
                data.TagId = obj.TagId;
                data.ModifiedAt = obj.ModifiedAt;
                data.DeleteFlag = obj.DeleteFlag;
            }
            else
            {
                failures.Add(obj.PhraseTagId, _notAppliedMessage);
            }
        }

        foreach (var obj in pushRequest.PushDto.Bookmarks)
        {
            if (obj.UserName != userName)
            {
                failures.Add(obj.BookmarkId, _unauthorizedExceptionMessage);
                continue;
            }

            var data = await db.Bookmarks.FindAsync(obj.BookmarkId);
            if (data is null)
            {
                await db.Bookmarks.AddAsync(obj.ToEntity());
            }
            else if (obj.ModifiedAt > data.ModifiedAt)
            {
                if (pushRequest.Overwrites.Contains(data.BookmarkId))
                {
                    await db.ConflictLogs.AddAsync(new ConflictLog
                    {
                        ConflictLogId = uuid.Generate(),
                        UserName = userName,
                        ClientId = clientId,
                        TargetId = data.BookmarkId,
                        Detail = data.ReadableSerialize(clientId),
                        ReportedAt = serverPushStartTime,
                    });
                }

                data.WordId = obj.WordId;
                data.Note = obj.Note;
                data.ModifiedAt = obj.ModifiedAt;
                data.DeleteFlag = obj.DeleteFlag;
            }
            else
            {
                failures.Add(obj.BookmarkId, _notAppliedMessage);
            }
        }

        foreach (var obj in pushRequest.PushDto.BookmarkTags)
        {
            var tag = await db.Tags.FindAsync(obj.TagId);
            var bookmark = await db.Bookmarks.FindAsync(obj.BookmarkId);
            if (tag is null)
            {
                failures.Add(obj.BookmarkTagId, "The `TagId` does not exist.");
                continue;
            }
            if (bookmark is null)
            {
                failures.Add(obj.BookmarkTagId, "The `BookmarkId` does not exist.");
                continue;
            }

            var tagCat = await db.TagCategories.FindAsync(tag.TagCategoryId);
            if (tagCat is null)
            {
                failures.Add(obj.BookmarkTagId, "The `TagCategoryId` does not exist.");
                continue;
            }

            if (tagCat.UserName != userName || bookmark.UserName != userName)
            {
                failures.Add(obj.BookmarkTagId, _unauthorizedExceptionMessage);
                continue;
            }

            var data = await db.BookmarkTags.FindAsync(obj.BookmarkTagId);
            if (data is null)
            {
                await db.BookmarkTags.AddAsync(obj.ToEntity());
            }
            else if (obj.ModifiedAt > data.ModifiedAt)
            {
                if (pushRequest.Overwrites.Contains(data.BookmarkTagId))
                {
                    await db.ConflictLogs.AddAsync(new ConflictLog
                    {
                        ConflictLogId = uuid.Generate(),
                        UserName = userName,
                        ClientId = clientId,
                        TargetId = data.BookmarkTagId,
                        Detail = data.ReadableSerialize(clientId),
                        ReportedAt = serverPushStartTime,
                    });
                }

                data.BookmarkId = obj.BookmarkId;
                data.TagId = obj.TagId;
                data.ModifiedAt = obj.ModifiedAt;
                data.DeleteFlag = obj.DeleteFlag;
            }
            else
            {
                failures.Add(obj.BookmarkTagId, _notAppliedMessage);
            }
        }

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateException ex)
        {
            var dbExceptions = new Dictionary<string, string>();
            foreach (var entry in ex.Entries)
            {
                var entityTypeName = entry.Entity.GetType().Name;
                var model = entry.Context.Model.FindEntityType(entry.Entity.GetType());
                var primaryKey = model?.FindPrimaryKey();

                if (primaryKey == null || primaryKey.Properties.Count != 1)
                    continue;

                var pkProperty = primaryKey.Properties[0];
                var pkValue = entry.Property(pkProperty.Name).CurrentValue?.ToString() ?? "null";

                var reason = ex.InnerException?.Message ?? ex.Message;
                dbExceptions.Add(pkValue, entityTypeName + ": " + reason);
            }

            return TypedResults.BadRequest(new SyncPushResponse(dbExceptions));
        }

        sync!.LastPush = serverPushStartTime;
        await db.SaveChangesAsync();

        return TypedResults.Ok(new SyncPushResponse(failures));
    }

    public static async Task<Results<Ok<SyncStatusDto>, NotFound>> GetSyncStatus
        ([FromRoute] string clientId, ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;
        var sync = await db.Users
            .WhereUser(userName)
            .SelectMany(u => u.Syncs)
            .FirstOrDefaultAsync(s => s.ClientId == clientId);

        if (sync is null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(new SyncStatusDto
        {
            LastPull = sync!.LastPull,
            LastPush = sync!.LastPush
        });
    }

    public static Ok<SyncServerTimeResponse> GetSyncServerTime(IDateTimeService dateTimeService)
    {
        return TypedResults.Ok(new SyncServerTimeResponse(dateTimeService.GetCurrentTicksString()));
    }

    private static string GetPluralName(string entityName)
    {
        return entityName switch
        {
            "Phrase" => "Phrases",
            "PhraseTag" => "PhraseTags",
            "Bookmark" => "Bookmarks",
            "BookmarkTag" => "BookmarkTags",
            "TagCategory" => "TagCategories",
            "Tag" => "Tags",
            _ => throw new NotImplementedException(),
        };
    }
}
