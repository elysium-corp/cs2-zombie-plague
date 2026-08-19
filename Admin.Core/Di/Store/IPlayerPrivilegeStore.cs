namespace Admin.Core.Di.Store;

internal interface IPlayerPrivilegeStore
{
    IReadOnlySet<string> Get(ulong steamId);

    void Set(ulong steamId, IEnumerable<string> privilegeKeys);

    void Remove(ulong steamId);
}