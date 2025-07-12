namespace BearWordsAPI.Shared.Data.Models;

public interface ISoftDeletable
{
    public bool DeleteFlag { get; set; }
}