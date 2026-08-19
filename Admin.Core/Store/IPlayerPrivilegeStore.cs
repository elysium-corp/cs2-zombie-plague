using Admin.Core.Data;

namespace Admin.Core.Store;

internal interface IPlayerPrivilegeStore
{
    IReadOnlyDictionary<string, PlayerPrivilege> Get(ulong steamId);

    void Set(ulong steamId, IEnumerable<PlayerPrivilege> privileges);

    void Remove(ulong steamId);
}