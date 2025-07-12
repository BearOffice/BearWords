namespace BearWordsMaui.Services.DbServices.DataItems;

public class ConflictContainer
{
    public required string ConflictLogId { get; set; }
    public required string TargetId { get; set; }
    public string? Detail { get; set; }
    public required long ReportedAt { get; set; }
    public ConflictTargetType TargetType { get; set; }
    public string? TargetDisplayName { get; set; }
}