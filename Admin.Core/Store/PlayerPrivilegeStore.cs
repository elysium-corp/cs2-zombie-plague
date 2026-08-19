using System.Collections.Concurrent;
using System.Collections.Frozen;
using Admin.Core.Data;

namespace Admin.Core.Store;

internal sealed class PlayerPrivilegeStore : IPlayerPrivilegeStore
{
    private static readonly FrozenDictionary<string, PlayerPrivilege> Empty =
        new Dictionary<string, PlayerPrivilege>().ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);

    private readonly ConcurrentDictionary<ulong, FrozenDictionary<string, PlayerPrivilege>> _privileges = new();

    public IReadOnlyDictionary<string, PlayerPrivilege> Get(ulong steamId)
    {
        return _privileges.GetValueOrDefault(steamId, Empty);
    }

    public void Set(ulong steamId, IEnumerable<PlayerPrivilege> privileges)
    {
        _privileges[steamId] = privileges.ToFrozenDictionary(x => x.Key, StringComparer.OrdinalIgnoreCase);
    }

    public void Remove(ulong steamId)
    {
        _privileges.TryRemove(steamId, out _);
    }
}