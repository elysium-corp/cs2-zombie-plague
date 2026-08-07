using System.Diagnostics.CodeAnalysis;
using ZombiePlague.Core.Store.Contracts;

namespace ZombiePlague.Core.Store;

internal sealed class InMemoryKeyValueStore : IKeyValueStore
{
    private readonly Dictionary<string, object> _values = [];

    public void Set<T>(string key, T value) where T : notnull
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        _values[key] = value;
    }

    public bool TryGet<T>(string key, [NotNullWhen(true)] out T? value) where T : notnull
    {
        if (_values.TryGetValue(key, out var storedValue) && storedValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    public bool Contains(string key)
    {
        return _values.ContainsKey(key);
    }

    public bool Remove(string key)
    {
        return _values.Remove(key);
    }

    public void Clear()
    {
        _values.Clear();
    }
}