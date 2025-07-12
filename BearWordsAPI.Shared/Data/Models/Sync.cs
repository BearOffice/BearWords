namespace BearWordsAPI.Shared.Data.Models;

public partial class Sync : IUserData
{
    public string UserName { get; set; } = null!;
    public string ClientId { get; set; } = null!;
    public long LastPull { get; set; }
    public long LastPush { get; set; }

    public virtual User UserNameNavigation { get; set; } = null!;
}
