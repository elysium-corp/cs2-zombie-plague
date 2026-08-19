using System.Collections.Concurrent;
using System.Collections.Frozen;

namespace Admin.Core.Store;

internal sealed class PlayerPrivilegeStore : IPlayerPrivilegeStore
{
    private static readonly FrozenSet<string> Empty = Array.Empty<string>().ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<ulong, FrozenSet<string>> _privileges = new();

    public IReadOnlySet<string> Get(ulong steamId)
    {
        return _privileges.GetValueOrDefault(steamId, Empty);
    }

    public void Set(ulong steamId, IEnumerable<string> privilegeKeys)
    {
        _privileges[steamId] = privilegeKeys.ToFrozenSet(StringComparer.OrdinalIgnoreCase);
    }

    public void Remove(ulong steamId)
    {
        _privileges.TryRemove(steamId, out _);
    }
}