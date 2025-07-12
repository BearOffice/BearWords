namespace BearWordsMaui.Services;

public interface ITempStorageService
{
    public void Push<T>(string id, T item);
    public T? Pop<T>(string id);
    public T? Peek<T>(string id);
    public bool Remove(string id);
    public bool RemoveAll();
    public bool Contains(string id);
    public int Count();

    public bool Protect(string id);
    public bool Unprotect(string id);
    public string[] GetProtectedIds();

    public bool IsProtected(string id)
    {
        return GetProtectedIds().Contains(id);
    }
}