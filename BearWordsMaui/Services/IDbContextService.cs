namespace BearWordsMaui.Services;

public interface IDbContextService
{
    public event EventHandler? BeforeSaveChanges;
    public BearWordsContext GetDbContext();
    public void CreateNewDbContext();
}
