using Admin.Api;
using Admin.Api.Data;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Application;

/// <summary>Fail-closed прокси необязательного Admin.Core.</summary>
internal sealed class ShopAdminApiProxy : IAdminApi
{
    private IAdminApi? _api;

    public void Initialize(IAdminApi api) => _api = api;

    public void Uninitialize() => _api = null;

    public IPrivilege? FindPrivilege(string key) => _api?.FindPrivilege(key);

    public IReadOnlyCollection<IPrivilege> GetPrivileges() => _api?.GetPrivileges() ?? [];

    public IReadOnlyCollection<IPrivilege> GetPlayerPrivileges(IPlayer player) =>
        _api?.GetPlayerPrivileges(player) ?? [];

    public bool HasPrivilege(IPlayer player, string privilegeKey) =>
        _api?.HasPrivilege(player, privilegeKey) ?? false;

    public bool HasPermission(IPlayer player, string permission) =>
        _api?.HasPermission(player, permission) ?? false;
}
