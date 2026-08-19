using Admin.Api;
using Admin.Api.Data;
using Admin.Core.Managers;
using Admin.Core.Registry;
using Admin.Core.Services;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Api;

internal sealed class AdminApi(
    IPrivilegeRegistry privilegeRegistry,
    IPrivilegeService privilegeService,
    PlayerPrivilegeManager playerPrivilegeManager) : IAdminApi
{
    public IPrivilege RegisterPrivilege(PrivilegeDefinition definition)
    {
        return privilegeRegistry.Register(definition);
    }

    public IPrivilege? FindPrivilege(string key)
    {
        return privilegeRegistry.Find(key);
    }

    public IReadOnlyCollection<IPrivilege> GetPrivileges()
    {
        return privilegeRegistry.GetAll();
    }

    public IReadOnlyCollection<IPrivilege> GetPlayerPrivileges(IPlayer player)
    {
        return privilegeService.GetPrivileges(player.SteamID);
    }

    public bool HasPrivilege(IPlayer player, string privilegeKey)
    {
        return privilegeService.HasPrivilege(player.SteamID, privilegeKey);
    }

    public bool HasPermission(IPlayer player, string permission)
    {
        return privilegeService.HasPermission(player.SteamID, permission);
    }
    
    public Task<bool> GrantPrivilegeAsync(
        ulong steamId,
        string privilegeKey,
        DateTime? expiresAtUtc = null)
    {
        return playerPrivilegeManager.GrantAsync(steamId, privilegeKey, expiresAtUtc);
    }

    public Task<bool> RevokePrivilegeAsync(ulong steamId, string privilegeKey)
    {
        return playerPrivilegeManager.RevokeAsync(steamId, privilegeKey);
    }
}