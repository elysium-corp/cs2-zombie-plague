using Admin.Api.Data;
using Admin.Core.Data;
using Admin.Core.Registry;
using Admin.Core.Store;

namespace Admin.Core.Services;

internal sealed class PrivilegeService(IPrivilegeRegistry privilegeRegistry, IPlayerPrivilegeStore playerPrivilegeStore) : IPrivilegeService
{
    public IReadOnlyCollection<IPrivilege> GetPrivileges(ulong steamId)
    {
        var result = new List<IPrivilege>();

        foreach (var playerPrivilege in playerPrivilegeStore.Get(steamId).Values)
        {
            if (!IsActive(playerPrivilege))
            {
                continue;
            }

            var privilege = privilegeRegistry.Find(playerPrivilege.Key);

            if (privilege != null)
            {
                result.Add(privilege);
            }
        }

        return result;
    }

    public bool HasPrivilege(ulong steamId, string privilegeKey)
    {
        return playerPrivilegeStore.Get(steamId).TryGetValue(privilegeKey, out var privilege) && IsActive(privilege);
    }

    public bool HasPermission(ulong steamId, string permission)
    {
        foreach (var playerPrivilege in playerPrivilegeStore.Get(steamId).Values)
        {
            if (!IsActive(playerPrivilege))
            {
                continue;
            }

            var privilege = privilegeRegistry.Find(playerPrivilege.Key);

            if (privilege?.Permissions.Contains(permission) == true)
            {
                return true;
            }
        }

        return false;
    }
    
    private static bool IsActive(PlayerPrivilege privilege)
    {
        return privilege.ExpiresAtUtc == null || privilege.ExpiresAtUtc > DateTime.UtcNow;
    }
}