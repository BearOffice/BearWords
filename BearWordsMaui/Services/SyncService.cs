using BearWordsAPI.Shared.Data.Models;
using BearWordsAPI.Shared.DTOs;
using BearWordsAPI.Shared.Helpers;
using BearWordsAPI.Shared.Services;
using BearWordsMaui.Data;
using BearWordsMaui.Helpers;
using EFCore.BulkExtensions;
using Microsoft.EntityFrameworkCore;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;

namespace BearWordsMaui.Services;

public record RegisterRequest(string ClientId);
public record SyncPullRequest(string LastPullTime);
public record SyncPullResponse(SyncPullDto PullDto);
public record SyncPushRequest(SyncPushDto PushDto, string[] Overwrites);
public record SyncPushResponse(Dictionary<string, string> Failures);
public record SyncServerTimeResponse(string Time);
public record ConflictsPullRequest(string LastPushTime);
public record ConflictsPullResponse(ConflictLogDto[] ConflictLogs);
public record ConflictsPushRequest(ConflictLogDto[] ConflictLogs);
public record ConflictsPushResponse(Dictionary<string, string> Failures);

public enum SyncStatus
{
    Pulling,
    Pushing,
    PullingAndPushingConflicts,
    Finished,
}

public class PullResults
{
    public required Dictionary<string, string> ChangesFromServer { get; init; }
    public required List<string> ServerConflictLogs { get; init; }
}

public class PullConflictsResults
{
    public required string[] ReceivedConflictsIds { get; init; }
}

public class SyncService : ISyncExecutor, ISyncUtils
{
    private readonly IApiHttpClientFactory _httpClientFactory;
    private readonly ConfigService _config;
    private readonly IDbContextService _dbContextService;
    private readonly IDateTimeService _dateTimeService;
    private readonly IUUIDGenerator _uuid;
    private readonly ILogService _log;
    private BearWordsContext Context => _dbContextService.GetDbContext();

    public SyncService(IApiHttpClientFactory httpClientFactory, ConfigService config,
        IDbContextService dbContextService, IDateTimeService dateTimeService, IUUIDGenerator uuid, ILogService log)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _dbContextService = dbContextService;
        _dateTimeService = dateTimeService;
        _uuid = uuid;
        _log = log;
    }

    public async Task<DateTime> GetServerTimeAsync()
    {
        var response = await _httpClientFactory.CreateSyncClient().GetAsync("syncs/server-time");
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Get server time failed: {response.StatusCode}.");
        }

        var result = await response.Content.ReadFromJsonAsync<SyncServerTimeResponse>();
        return result!.Time.ToDateTime();
    }

    public async Task<bool> RegisterClientAsync(string clientId)
    {
        var response = await _httpClientFactory.CreateSyncClient()
            .PostAsJsonAsync($"register", new RegisterRequest(clientId));

        if (response.IsSuccessStatusCode)
        {
            return true;
        }
        else
        {
            if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                return false;
            }
            else
            {
                throw new HttpRequestException($"Register client failed: {response.StatusCode}.");
            }
        }
    }

    public DateTime? GetLastSyncDateTime()
    {
        return _config.LastSync;
    }

    public string[] GetToPushItemIds()
    {
        var clientSync = Context.Syncs
            .AsNoTracking()
            .FirstOrDefault(s => s.ClientId == _config.ClientId)!;
        var lastPushTime = clientSync.LastPush;

        var changes = Context.Phrases
            .AsNoTracking()
            .Where(p => p.ModifiedAt > lastPushTime)
            .Select(p => p.PhraseId)
            .ToList();
        changes.AddRange(Context.PhraseTags
            .AsNoTracking()
            .Where(p => p.ModifiedAt > lastPushTime)
            .Select(p => p.PhraseTagId));
        changes.AddRange(Context.Bookmarks
            .AsNoTracking()
            .Where(b => b.ModifiedAt > lastPushTime)
            .Select(b => b.BookmarkId));
        changes.AddRange(Context.BookmarkTags
            .AsNoTracking()
            .Where(b => b.ModifiedAt > lastPushTime)
            .Select(b => b.BookmarkTagId));
        changes.AddRange(Context.Tags
            .AsNoTracking()
            .Where(t => t.ModifiedAt > lastPushTime)
            .Select(t => t.TagId));
        changes.AddRange(Context.TagCategories
            .AsNoTracking()
            .Where(t => t.ModifiedAt > lastPushTime)
            .Select(t => t.TagCategoryId));

        return [.. changes];
    }

    public int GetToPushItemCount()
    {
        var clientSync = Context.Syncs
            .AsNoTracking()
            .FirstOrDefault(s => s.ClientId == _config.ClientId)!;
        var lastPushTime = clientSync.LastPush;

        var count = Context.Phrases
            .AsNoTracking()
            .Where(p => p.ModifiedAt > lastPushTime)
            .Select(p => p.PhraseId)
            .Count();
        count += Context.PhraseTags
            .AsNoTracking()
            .Where(p => p.ModifiedAt > lastPushTime)
            .Select(p => p.PhraseTagId)
            .Count();
        count += Context.Bookmarks
            .AsNoTracking()
            .Where(b => b.ModifiedAt > lastPushTime)
            .Select(b => b.BookmarkId)
            .Count();
        count += Context.BookmarkTags
            .AsNoTracking()
            .Where(b => b.ModifiedAt > lastPushTime)
            .Select(b => b.BookmarkTagId)
            .Count();
        count += Context.Tags
            .AsNoTracking()
            .Where(t => t.ModifiedAt > lastPushTime)
            .Select(t => t.TagId)
            .Count();
        count += Context.TagCategories
            .AsNoTracking()
            .Where(t => t.ModifiedAt > lastPushTime)
            .Select(t => t.TagCategoryId)
            .Count();

        return count;
    }

    public int GetLastPullItemCount()
    {
        return _config.LastPullItemCount;
    }

    public int GetLastPushItemCount()
    {
        return _config.LastPushItemCount;
    }

    public async Task SyncAsync()
    {
        _config.LastSync = _dateTimeService.GetCurrent();
        _config.LastPullItemCount = 0;
        _config.LastPushItemCount = 0;

        // Try registering the client again, as the initial registration may have failed.
        await RegisterClientAsync(_config.ClientId);

        if (_config.SyncStatus != SyncStatus.Finished)
        {
            var cResults = await PullConflictsAsync();
            await PushConflictsAsync(cResults);

            _config.SyncStatus = SyncStatus.Finished;
        }

        _config.SyncStatus = SyncStatus.Pulling;
        var pullResults = await PullAsync();

        _config.SyncStatus = SyncStatus.Pushing;
        await PushAsync(pullResults);

        _config.SyncStatus = SyncStatus.PullingAndPushingConflicts;
        var conflictResults = await PullConflictsAsync();
        await PushConflictsAsync(conflictResults);

        _config.SyncStatus = SyncStatus.Finished;
    }

    public async Task<PullResults> PullAsync()
    {
        var clientId = _config.ClientId;
        var userName = _config.UserName;

        var clientPullStartTime = _dateTimeService.GetCurrentTicksLong();
        var clientSync = Context.Syncs.FirstOrDefault(s => s.ClientId == clientId)!;

        var syncPullResponse = await PullApi(new SyncPullRequest(clientSync.LastPull.ToDateTimeText()));
        var pullDto = (syncPullResponse?.PullDto) ?? throw new Exception("Failed to get PullDto from server.");

        // === Add language, dictionary, translation ===
        // Language
        foreach (var obj in pullDto.Languages)
        {
            var local = Context.Languages.Find(obj.LanguageCode);
            if (local is null)
            {
                Context.Languages.Add(obj.ToEntity());
            }
            else
            {
                if (local.LanguageName != obj.LanguageName)
                {
                    local.LanguageName = obj.LanguageName;
                }
            }
        }
        Context.SaveChanges();

        // Dictionary
        var newDics = pullDto.Dictionaries.Select(obj => new Dictionary
        {
            WordId = obj.WordId,
            Word = obj.Word,
            SourceLanguage = obj.SourceLanguage,
            Pronounce = obj.Pronounce,
            ModifiedAt = obj.ModifiedAt,
            DeleteFlag = obj.DeleteFlag
        }).ToList();

        Context.BulkInsertOrUpdate(newDics);

        // Translation
        var newtrans = pullDto.Translations.Select(obj => new Translation
        {
            TranslationId = obj.TranslationId,
            WordId = obj.WordId,
            TargetLanguage = obj.TargetLanguage,
            TranslationText = obj.TranslationText,
            ModifiedAt = obj.ModifiedAt,
            DeleteFlag = obj.DeleteFlag,
        }).ToList();

        Context.BulkInsertOrUpdate(newtrans);


        // === Add other data ===
        var localChangeIds = GetToPushItemIds();
        var changesFromServer = new Dictionary<string, string>();
        var serverConflictLog = new List<string>();

        foreach (var obj in pullDto.TagCategories)
        {
            var local = Context.TagCategories.Find(obj.TagCategoryId);
            if (local is null)
            {
                Context.TagCategories.Add(obj.ToEntity());
                changesFromServer[obj.TagCategoryId] = obj.ModifiedAt.ToDateTimeText();
            }
            else
            {
                if (localChangeIds.Contains(obj.TagCategoryId))
                {
                    if (local.ModifiedAt > obj.ModifiedAt)
                    {
                        serverConflictLog.Add(obj.TagCategoryId);
                        continue;
                    }
                    else
                    {
                        Context.ConflictLogs.Add(new ConflictLog
                        {
                            ConflictLogId = _uuid.Generate(),
                            UserName = userName,
                            ClientId = clientId,
                            TargetId = local.TagCategoryId,
                            Detail = local.ReadableSerialize(clientId),
                            ReportedAt = clientPullStartTime,
                        });
                    }
                }

                local.CategoryName = obj.CategoryName;
                local.Description = obj.Description;
                local.ModifiedAt = obj.ModifiedAt;
                local.DeleteFlag = obj.DeleteFlag;

                changesFromServer[obj.TagCategoryId] = obj.ModifiedAt.ToDateTimeText();
            }
        }

        foreach (var obj in pullDto.Tags)
        {
            var local = Context.Tags.Find(obj.TagId);
            if (local is null)
            {
                Context.Tags.Add(obj.ToEntity());
                changesFromServer[obj.TagId] = obj.ModifiedAt.ToDateTimeText();
            }
            else
            {
                if (localChangeIds.Contains(obj.TagId))
                {
                    if (local.ModifiedAt > obj.ModifiedAt)
                    {
                        serverConflictLog.Add(obj.TagId);
                        continue;
                    }
                    else
                    {
                        Context.ConflictLogs.Add(new ConflictLog
                        {
                            ConflictLogId = _uuid.Generate(),
                            UserName = userName,
                            ClientId = clientId,
                            TargetId = local.TagId,
                            Detail = local.ReadableSerialize(clientId),
                            ReportedAt = clientPullStartTime,
                        });
                    }
                }

                local.TagName = obj.TagName;
                local.TagCategoryId = obj.TagCategoryId;
                local.Description = obj.Description;
                local.ModifiedAt = obj.ModifiedAt;
                local.DeleteFlag = obj.DeleteFlag;

                changesFromServer[obj.TagId] = obj.ModifiedAt.ToDateTimeText();
            }
        }

        foreach (var obj in pullDto.Phrases)
        {
            var local = Context.Phrases.Find(obj.PhraseId);
            if (local is null)
            {
                Context.Phrases.Add(obj.ToEntity());
                changesFromServer[obj.PhraseId] = obj.ModifiedAt.ToDateTimeText();
            }
            else
            {
                if (localChangeIds.Contains(obj.PhraseId))
                {
                    if (local.ModifiedAt > obj.ModifiedAt)
                    {
                        serverConflictLog.Add(obj.PhraseId);
                        continue;
                    }
                    else
                    {
                        Context.ConflictLogs.Add(new ConflictLog
                        {
                            ConflictLogId = _uuid.Generate(),
                            UserName = userName,
                            ClientId = clientId,
                            TargetId = local.PhraseId,
                            Detail = local.ReadableSerialize(clientId),
                            ReportedAt = clientPullStartTime,
                        });
                    }
                }

                local.PhraseText = obj.PhraseText;
                local.PhraseLanguage = obj.PhraseLanguage;
                local.Note = obj.Note;
                local.ModifiedAt = obj.ModifiedAt;
                local.DeleteFlag = obj.DeleteFlag;

                changesFromServer[obj.PhraseId] = obj.ModifiedAt.ToDateTimeText();
            }
        }

        foreach (var obj in pullDto.PhraseTags)
        {
            var local = Context.PhraseTags.Find(obj.PhraseTagId);
            if (local is null)
            {
                Context.PhraseTags.Add(obj.ToEntity());
                changesFromServer[obj.PhraseTagId] = obj.ModifiedAt.ToDateTimeText();
            }
            else
            {
                if (localChangeIds.Contains(obj.PhraseTagId))
                {
                    if (local.ModifiedAt > obj.ModifiedAt)
                    {
                        serverConflictLog.Add(obj.PhraseTagId);
                        continue;
                    }
                    else
                    {
                        Context.ConflictLogs.Add(new ConflictLog
                        {
                            ConflictLogId = _uuid.Generate(),
                            UserName = userName,
                            ClientId = clientId,
                            TargetId = local.PhraseTagId,
                            Detail = local.ReadableSerialize(clientId),
                            ReportedAt = clientPullStartTime,
                        });
                    }
                }

                local.PhraseId = obj.PhraseId;
                local.TagId = obj.TagId;
                local.ModifiedAt = obj.ModifiedAt;
                local.DeleteFlag = obj.DeleteFlag;

                changesFromServer[obj.PhraseTagId] = obj.ModifiedAt.ToDateTimeText();
            }
        }

        foreach (var obj in pullDto.Bookmarks)
        {
            var local = Context.Bookmarks.Find(obj.BookmarkId);
            if (local is null)
            {
                Context.Bookmarks.Add(obj.ToEntity());
                changesFromServer[obj.BookmarkId] = obj.ModifiedAt.ToDateTimeText();
            }
            else
            {
                if (localChangeIds.Contains(obj.BookmarkId))
                {
                    if (local.ModifiedAt > obj.ModifiedAt)
                    {
                        serverConflictLog.Add(obj.BookmarkId);
                        continue;
                    }
                    else
                    {
                        Context.ConflictLogs.Add(new ConflictLog
                        {
                            ConflictLogId = _uuid.Generate(),
                            UserName = userName,
                            ClientId = clientId,
                            TargetId = local.BookmarkId,
                            Detail = local.ReadableSerialize(clientId),
                            ReportedAt = clientPullStartTime,
                        });
                    }
                }

                local.WordId = obj.WordId;
                local.Note = obj.Note;
                local.ModifiedAt = obj.ModifiedAt;
                local.DeleteFlag = obj.DeleteFlag;

                changesFromServer[obj.BookmarkId] = obj.ModifiedAt.ToDateTimeText();
            }
        }

        foreach (var obj in pullDto.BookmarkTags)
        {
            var local = Context.BookmarkTags.Find(obj.BookmarkTagId);
            if (local is null)
            {
                Context.BookmarkTags.Add(obj.ToEntity());
                changesFromServer[obj.BookmarkTagId] = obj.ModifiedAt.ToDateTimeText();
            }
            else
            {
                if (localChangeIds.Contains(obj.BookmarkTagId))
                {
                    if (local.ModifiedAt > obj.ModifiedAt)
                    {
                        serverConflictLog.Add(obj.BookmarkTagId);
                        continue;
                    }
                    else
                    {
                        Context.ConflictLogs.Add(new ConflictLog
                        {
                            ConflictLogId = _uuid.Generate(),
                            UserName = userName,
                            ClientId = clientId,
                            TargetId = local.BookmarkTagId,
                            Detail = local.ReadableSerialize(clientId),
                            ReportedAt = clientPullStartTime,
                        });
                    }
                }

                local.BookmarkId = obj.BookmarkId;
                local.TagId = obj.TagId;
                local.ModifiedAt = obj.ModifiedAt;
                local.DeleteFlag = obj.DeleteFlag;

                changesFromServer[obj.BookmarkTagId] = obj.ModifiedAt.ToDateTimeText();
            }
        }


        clientSync.LastPull = clientPullStartTime;
        Context.SaveChanges();

        // Record pull item num
        _config.LastPullItemCount = CountPullDtoItems(pullDto);

        return new PullResults
        {
            ChangesFromServer = changesFromServer,
            ServerConflictLogs = serverConflictLog
        };
    }

    public async Task PushAsync(PullResults pullResults)
    {
        var clientPushStartTime = _dateTimeService.GetCurrentTicksLong();
        var clientSync = Context.Syncs.FirstOrDefault(s => s.ClientId == _config.ClientId)!;
        var lastPushTime = clientSync.LastPush;

        var localChangeIds = GetToPushItemIds();
        var pushDto = new SyncPushDto
        {
            Phrases = localChangeIds
                .Select(id => Context.Phrases.Find(id))
                .Where(o => o is not null)
                .Where(o => !(pullResults.ChangesFromServer.ContainsKey(o!.PhraseId) &&
                            pullResults.ChangesFromServer[o.PhraseId].ToDateTimeLong() == o.ModifiedAt))
                .Select(o => new PhraseDto(o!))
                .ToArray(),
            PhraseTags = localChangeIds
                .Select(id => Context.PhraseTags.Find(id))
                .Where(o => o is not null)
                .Where(o => !(pullResults.ChangesFromServer.ContainsKey(o!.PhraseTagId) &&
                            pullResults.ChangesFromServer[o.PhraseTagId].ToDateTimeLong() == o.ModifiedAt))
                .Select(o => new PhraseTagDto(o!))
                .ToArray(),
            Bookmarks = localChangeIds
                .Select(id => Context.Bookmarks.Find(id))
                .Where(o => o is not null)
                .Where(o => !(pullResults.ChangesFromServer.ContainsKey(o!.BookmarkId) &&
                            pullResults.ChangesFromServer[o.BookmarkId].ToDateTimeLong() == o.ModifiedAt))
                .Select(o => new BookmarkDto(o!))
                .ToArray(),
            BookmarkTags = localChangeIds
                .Select(id => Context.BookmarkTags.Find(id))
                .Where(o => o is not null)
                .Where(o => !(pullResults.ChangesFromServer.ContainsKey(o!.BookmarkTagId) &&
                            pullResults.ChangesFromServer[o.BookmarkTagId].ToDateTimeLong() == o.ModifiedAt))
                .Select(o => new BookmarkTagDto(o!))
                .ToArray(),
            TagCategories = localChangeIds
                .Select(id => Context.TagCategories.Find(id))
                .Where(o => o is not null)
                .Where(o => !(pullResults.ChangesFromServer.ContainsKey(o!.TagCategoryId) &&
                            pullResults.ChangesFromServer[o.TagCategoryId].ToDateTimeLong() == o.ModifiedAt))
                .Select(o => new TagCategoryDto(o!))
                .ToArray(),
            Tags = localChangeIds
                .Select(id => Context.Tags.Find(id))
                .Where(o => o is not null)
                .Where(o => !(pullResults.ChangesFromServer.ContainsKey(o!.TagId) &&
                            pullResults.ChangesFromServer[o.TagId].ToDateTimeLong() == o.ModifiedAt))
                .Select(o => new TagDto(o!))
                .ToArray()
        };

        var syncPushResponse = await PushApi(new SyncPushRequest(pushDto, [.. pullResults.ServerConflictLogs]));
        WriteFailuresToLogs(syncPushResponse?.Failures);

        clientSync.LastPush = clientPushStartTime;
        Context.SaveChanges();

        // Record push item num
        _config.LastPushItemCount = CountPushDtoItems(pushDto);
    }

    public async Task<PullConflictsResults> PullConflictsAsync()
    {
        var clientSync = Context.Syncs.FirstOrDefault(s => s.ClientId == _config.ClientId)!;
        var conflictsPullResponse = await ConflictsPullApi(
            new ConflictsPullRequest(clientSync.LastPush.ToDateTime().ToText()));

        foreach (var obj in conflictsPullResponse!.ConflictLogs)
        {
            var local = Context.ConflictLogs.Find(obj.ConflictLogId);
            if (local is null)
            {
                Context.ConflictLogs.Add(obj.ToEntity());
            }
            else
            {
                local.TargetId = obj.TargetId;
                local.Detail = obj.Detail;
                local.ReportedAt = obj.ReportedAt;
            }
        }

        Context.SaveChanges();

        return new PullConflictsResults
        {
            ReceivedConflictsIds = [.. conflictsPullResponse.ConflictLogs.Select(c => c.ConflictLogId)]
        };
    }

    public async Task PushConflictsAsync(PullConflictsResults pullConflictsResults)
    {
        var clientSync = Context.Syncs.FirstOrDefault(s => s.ClientId == _config.ClientId)!;
        var clientLastPull = clientSync.LastPull;

        var conflictToSend = Context.ConflictLogs
            .AsNoTracking()
            .Where(c => c.ReportedAt >= clientLastPull)
            .Where(c => !pullConflictsResults.ReceivedConflictsIds.Contains(c.ConflictLogId))
            .Select(c => new ConflictLogDto(c));

        var conflictsPushResponse = await ConflictsPushApi(new ConflictsPushRequest([.. conflictToSend]));
        WriteFailuresToLogs(conflictsPushResponse?.Failures);
    }

    private async Task<SyncPullResponse?> PullApi(SyncPullRequest request)
    {
        var response = await _httpClientFactory.CreateSyncClient()
            .PostAsJsonAsync($"syncs/{_config.ClientId}/pull", request);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Pull failed: {response.StatusCode}.");
        }

        return await response.Content.ReadFromJsonAsync<SyncPullResponse>();
    }

    private async Task<SyncPushResponse?> PushApi(SyncPushRequest request)
    {
        var response = await _httpClientFactory.CreateSyncClient()
            .PostAsJsonAsync($"syncs/{_config.ClientId}/push", request);
        if (!response.IsSuccessStatusCode)
        {
            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var content = await response.Content.ReadFromJsonAsync<SyncPushResponse>();
                WriteFailuresToLogs(content?.Failures);
            }
            throw new HttpRequestException($"Push failed: {response.StatusCode}.");
        }

        return await response.Content.ReadFromJsonAsync<SyncPushResponse>();
    }

    private async Task<ConflictsPullResponse?> ConflictsPullApi(ConflictsPullRequest request)
    {
        var response = await _httpClientFactory.CreateSyncClient()
            .PostAsJsonAsync($"conflicts/{_config.ClientId}/pull", request);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Pull conflicts failed: {response.StatusCode}.");
        }

        return await response.Content.ReadFromJsonAsync<ConflictsPullResponse>();
    }

    private async Task<ConflictsPushResponse?> ConflictsPushApi(ConflictsPushRequest request)
    {
        var response = await _httpClientFactory.CreateSyncClient()
            .PostAsJsonAsync($"conflicts/{_config.ClientId}/push", request);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Push conflicts failed: {response.StatusCode}.");
        }

        return await response.Content.ReadFromJsonAsync<ConflictsPushResponse>();
    }

    private void WriteFailuresToLogs(Dictionary<string, string>? failures)
    {
        if (failures is null) return;

        foreach ((var key, var value) in failures)
        {
            _log.Warn($"Failures  {key}: {value}");
        }
    }

    private static int CountPullDtoItems(SyncPullDto syncPullDto)
    {
        return syncPullDto.Dictionaries.Length +
               syncPullDto.Translations.Length +
               syncPullDto.Phrases.Length +
               syncPullDto.PhraseTags.Length +
               syncPullDto.Bookmarks.Length +
               syncPullDto.BookmarkTags.Length +
               syncPullDto.TagCategories.Length +
               syncPullDto.Tags.Length;
    }

    private static int CountPushDtoItems(SyncPushDto syncPushDto)
    {
        return syncPushDto.Phrases.Length +
               syncPushDto.PhraseTags.Length +
               syncPushDto.Bookmarks.Length +
               syncPushDto.BookmarkTags.Length +
               syncPushDto.TagCategories.Length +
               syncPushDto.Tags.Length;
    }
}