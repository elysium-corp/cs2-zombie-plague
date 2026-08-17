using System.Collections.Concurrent;
using Common.Database.Sessions;

namespace Common.Database.Storages;

public sealed class PlayerSessionStore<TData> where TData : class
{
    private readonly ConcurrentDictionary<ulong, PersistentSession<TData>> _sessions = new();

    public PersistentSession<TData> Create(ulong steamId, TData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        var session = new PersistentSession<TData>(data);

        _sessions[steamId] = session;

        return session;
    }

    public PersistentSession<TData>? Get(ulong steamId)
    {
        return _sessions.GetValueOrDefault(steamId);
    }

    public bool IsCurrent(ulong steamId, PersistentSession<TData> session)
    {
        return _sessions.TryGetValue(steamId, out var current) && ReferenceEquals(current, session);
    }

    public bool TryRemove(ulong steamId, out PersistentSession<TData>? session)
    {
        return _sessions.TryRemove(steamId, out session);
    }

    public IReadOnlyCollection<KeyValuePair<ulong, PersistentSession<TData>>> GetAll()
    {
        return _sessions.ToArray();
    }
}