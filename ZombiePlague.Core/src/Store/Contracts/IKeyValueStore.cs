using System.Diagnostics.CodeAnalysis;

namespace ZombiePlague.Core.Store.Contracts;

public interface IKeyValueStore
{
    void Set<T>(string key, T value) where T : notnull;

    bool TryGet<T>(string key, [NotNullWhen(true)] out T? value) where T : notnull;

    bool Contains(string key);

    bool Remove(string key);

    void Clear();
}