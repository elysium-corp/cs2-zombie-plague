using Admin.Api.Data;
using Admin.Core.Di.Store;
using Admin.Core.Registry;

namespace Admin.Core.Services;

internal sealed class PrivilegeService(IPrivilegeRegistry privilegeRegistry, IPlayerPrivilegeStore playerPrivilegeStore) : IPrivilegeService
{
    public IReadOnlyCollection<IPrivilege> GetPrivileges(ulong steamId)
    {
        var result = new List<IPrivilege>();

        foreach (var key in playerPrivilegeStore.Get(steamId))
        {
            var privilege = privilegeRegistry.Find(key);

            if (privilege != null)
            {
                result.Add(privilege);
            }
        }

        return result;
    }

    public bool HasPrivilege(ulong steamId, string privilegeKey)
    {
        return playerPrivilegeStore.Get(steamId).Contains(privilegeKey);
    }

    public bool HasPermission(ulong steamId, string permission)
    {
        foreach (var key in playerPrivilegeStore.Get(steamId))
        {
            var privilege = privilegeRegistry.Find(key);

            if (privilege?.Permissions.Contains(permission) == true)
            {
                return true;
            }
        }

        return false;
    }
}