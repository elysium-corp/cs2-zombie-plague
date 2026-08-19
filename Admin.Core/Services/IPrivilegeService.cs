using Admin.Api.Data;

namespace Admin.Core.Services;

internal interface IPrivilegeService
{
    IReadOnlyCollection<IPrivilege> GetPrivileges(ulong steamId);

    bool HasPrivilege(ulong steamId, string privilegeKey);

    bool HasPermission(ulong steamId, string permission);
}