using BearWordsAPI.Shared.Data;
using BearWordsAPI.Shared.DTOs;
using BearWordsAPI.Shared.Helpers;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BearWordsAPI.RequestHandler;

public record ConflictsResponse(ConflictLogDto[] ConflictLogs);
public record ConflictsPullRequest(string LastPushTime);
public record ConflictsPullResponse(ConflictLogDto[] ConflictLogs);
public record ConflictsPushRequest(ConflictLogDto[] ConflictLogs);
public record ConflictsPushResponse(Dictionary<string, string> Failures);

public static class ConflictsHandler
{
    private const string _unauthorizedExceptionMessage = "Unauthorized operation.";
    private const string _notAppliedExceptionMessage = "Conflict already logged in server.";

    public static async Task<Ok<ConflictsResponse>> GetConflicts(ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;
        var logs = await db.Users
            .WhereUser(userName)
            .SelectMany(u => u.ConflictLogs)
            .ToArrayAsync();

        return TypedResults.Ok(new ConflictsResponse([.. logs.Select(c => new ConflictLogDto(c))]));
    }

    public static async Task<Ok<ConflictsPullResponse>> PostConflictsPull
        ([FromRoute] string clientId, [FromBody] ConflictsPullRequest pullRequest,
            ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name!;
        var sync = await db.Users
            .WhereUser(userName)
            .SelectMany(u => u.Syncs)
            .FirstOrDefaultAsync(s => s.ClientId == clientId);

        var clientLastPushTime = pullRequest.LastPushTime.ToDateTime().ToLong();
        var serverLastPushTime = sync!.LastPush;
        var lastPushTime = serverLastPushTime <= clientLastPushTime ? serverLastPushTime : clientLastPushTime;

        var logs = await db.Users
            .WhereUser(userName)
            .SelectMany(u => u.ConflictLogs)
            .ToArrayAsync();

        return TypedResults.Ok(new ConflictsPullResponse(
            [.. logs.Where(l => l.ReportedAt >= lastPushTime)
                    .Select(l => new ConflictLogDto(l))]
        ));
    }

    public static async Task<Ok<ConflictsPushResponse>> PostConflictsPush
        ([FromRoute] string clientId, [FromBody] ConflictsPushRequest pushRequest,
            ClaimsPrincipal user, BearWordsContext db)
    {
        var userName = user.Identity!.Name;

        var failures = new Dictionary<string, string>();

        foreach (var obj in pushRequest.ConflictLogs)
        {
            if (obj.UserName != userName || obj.ClientId != clientId)
            {
                failures.Add(obj.ConflictLogId, _unauthorizedExceptionMessage);
                continue;
            }

            var data = await db.ConflictLogs.FindAsync(obj.ConflictLogId);
            if (data is null)
            {
                await db.ConflictLogs.AddAsync(obj.ToEntity());
            }
            else
            {
                failures.Add(obj.ConflictLogId, _notAppliedExceptionMessage);
            }
        }

        await db.SaveChangesAsync();

        return TypedResults.Ok(new ConflictsPushResponse(failures));
    }
}
