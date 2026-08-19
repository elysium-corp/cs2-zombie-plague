using Admin.Api.Data;
using SwiftlyS2.Shared.Players;

namespace Admin.Api;

public interface IAdminApi
{
    IPrivilege RegisterPrivilege(PrivilegeDefinition definition);

    IPrivilege? FindPrivilege(string key);

    IReadOnlyCollection<IPrivilege> GetPrivileges();

    IReadOnlyCollection<IPrivilege> GetPlayerPrivileges(IPlayer player);

    bool HasPrivilege(IPlayer player, string privilegeKey);

    bool HasPermission(IPlayer player, string permission);
    
    Task<bool> GrantPrivilegeAsync(ulong steamId, string privilegeKey, DateTime? expiresAtUtc = null);

    Task<bool> RevokePrivilegeAsync(ulong steamId, string privilegeKey);
    
    Task<PlayerPrivilegeInfo?> FindPlayerPrivilegeAsync(ulong steamId, string privilegeKey);

    Task<bool> ExtendPrivilegeAsync(ulong steamId, string privilegeKey, TimeSpan duration);

    public static readonly string SharedApiKey = "Admin.Api.IAdminApi";
}