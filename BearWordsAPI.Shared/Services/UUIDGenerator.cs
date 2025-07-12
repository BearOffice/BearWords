namespace BearWordsAPI.Shared.Services;

public class UUIDGenerator : IUUIDGenerator
{
    public string Generate()
    {
        return Guid.NewGuid().ToString();
    }
}
