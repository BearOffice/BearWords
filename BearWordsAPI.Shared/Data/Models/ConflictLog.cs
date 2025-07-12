namespace BearWordsAPI.Shared.Data.Models;

public partial class ConflictLog: IUserData
{
    public string ConflictLogId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public string TargetId { get; set; } = null!;
    public string? Detail { get; set; }
    public long ReportedAt { get; set; }

    public virtual User UserNameNavigation { get; set; } = null!;
}
