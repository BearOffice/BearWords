using System.Collections.Concurrent;

namespace BearWordsMaui.Services;

public class TempStorageService : ITempStorageService
{
    private readonly ConcurrentDictionary<string, object?> _storage = [];
    private readonly ConcurrentDictionary<string, bool> _protected = [];

    public void Push<T>(string id, T item)
    {
        _storage[id] = item;
    }

    public T? Pop<T>(string id)
    {
        if (_protected.ContainsKey(id))
            throw new InvalidOperationException($"`{id}` is protected.");
        if (_storage.TryRemove(id, out var value))
        {
            return value is null ? default : (T)value;
        }
        return default;
    }

    public T? Peek<T>(string id)
    {
        if (_storage.TryGetValue(id, out var value))
        {
            return value is null ? default : (T)value;
        }
        return default;
    }

    public bool Remove(string id)
    {
        if (_protected.ContainsKey(id))
            throw new InvalidOperationException($"`{id}` is protected.");
        return _storage.TryRemove(id, out _);
    }

    public bool RemoveAll()
    {
        var isSucceeded = true;
        var keysToRemove = _storage.Keys.Where(key => !_protected.ContainsKey(key));
        foreach (var key in keysToRemove)
        {
            var result = _storage.TryRemove(key, out _);
            if (isSucceeded) isSucceeded = result;
        }

        return isSucceeded;
    }

    public bool Contains(string id)
    {
        return _storage.ContainsKey(id);
    }

    public int Count()
    {
        return _storage.Count;
    }

    public bool Protect(string id)
    {
        if (_storage.ContainsKey(id))
        {
            _protected[id] = true;
            return true;
        }
        return false;
    }

    public bool Unprotect(string id)
    {
        return _protected.TryRemove(id, out _);
    }

    public string[] GetProtectedIds()
    {
        return [.. _protected.Keys];
    }
}