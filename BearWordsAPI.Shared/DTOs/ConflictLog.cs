using BearWordsAPI.Shared.Data.Models;

namespace BearWordsAPI.Shared.DTOs;

public class ConflictLogDto
{
    public string ConflictLogId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string TargetId { get; set; } = null!;
    public string? Detail { get; set; }
    public long ReportedAt { get; set; }

    public ConflictLogDto() { }

    public ConflictLogDto(ConflictLog conflictLog)
    {
        ConflictLogId = conflictLog.ConflictLogId;
        UserName = conflictLog.UserName;
        ClientId = conflictLog.ClientId;
        TargetId = conflictLog.TargetId;
        Detail = conflictLog.Detail;
        ReportedAt = conflictLog.ReportedAt;
    }

    public ConflictLog ToEntity()
    {
        return new ConflictLog
        {
            ConflictLogId = ConflictLogId,
            UserName = UserName,
            ClientId = ClientId,
            TargetId = TargetId,
            Detail = Detail,
            ReportedAt = ReportedAt
        };
    }
}
