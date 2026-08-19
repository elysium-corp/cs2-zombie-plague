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

    public static readonly string SharedApiKey = "Admin.Api.IAdminApi";
}