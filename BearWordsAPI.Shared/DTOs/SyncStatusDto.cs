namespace BearWordsAPI.Shared.DTOs;

public class SyncStatusDto
{
    public long LastPull { get; set; }
    public long LastPush { get; set; }
}
