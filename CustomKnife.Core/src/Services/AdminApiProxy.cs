using Admin.Api;
using Admin.Api.Data;
using SwiftlyS2.Shared.Players;

namespace CustomKnife.Services;

/// <summary>
/// Предоставляет fail-closed доступ к необязательному Shared API Admin.Core.
/// </summary>
internal sealed class AdminApiProxy : IAdminApi
{
    private IAdminApi? _api;

    public void Initialize(IAdminApi api)
    {
        _api = api;
    }

    public void Uninitialize()
    {
        _api = null;
    }

    public IPrivilege? FindPrivilege(string key)
    {
        return _api?.FindPrivilege(key);
    }

    public IReadOnlyCollection<IPrivilege> GetPrivileges()
    {
        return _api?.GetPrivileges() ?? [];
    }

    public IReadOnlyCollection<IPrivilege> GetPlayerPrivileges(IPlayer player)
    {
        return _api?.GetPlayerPrivileges(player) ?? [];
    }

    public bool HasPrivilege(IPlayer player, string privilegeKey)
    {
        return _api?.HasPrivilege(player, privilegeKey) ?? false;
    }

    public bool HasPermission(IPlayer player, string permission)
    {
        return _api?.HasPermission(player, permission) ?? false;
    }
}
