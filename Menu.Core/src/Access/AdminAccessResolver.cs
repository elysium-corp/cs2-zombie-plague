using Admin.Api;
using Menu.Api.Contracts;
using Menu.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace Menu.Core.Access;

/// <summary>
/// Выполняет только runtime-проверки прав через in-memory API Admin.Core.
/// </summary>
internal sealed class AdminAccessResolver
{
    private IAdminApi? _adminApi;

    internal bool IsAvailable => Volatile.Read(ref _adminApi) is not null;

    internal void Bind(IAdminApi? adminApi) => Interlocked.Exchange(ref _adminApi, adminApi);

    internal bool HasPermission(IPlayer player, string permission)
    {
        if (player is null || string.IsNullOrWhiteSpace(permission))
        {
            return false;
        }

        var adminApi = Volatile.Read(ref _adminApi);
        if (adminApi is null)
        {
            return false;
        }

        try
        {
            return adminApi.HasPermission(player, permission);
        }
        catch
        {
            // Защищённый доступ намеренно закрывается при выгрузке, неготовности
            // или неожиданной runtime-ошибке Admin.Core.
            return false;
        }
    }

    internal bool CanAccess(
        IPlayer player,
        MenuAccessPolicyDefinition? policy,
        MenuAccessPolicyDefinition? inheritedPolicy = null)
    {
        var effective = policy ?? new MenuAccessPolicyDefinition();
        if (effective.Kind == MenuAccessPolicyKind.Inherited)
        {
            if (inheritedPolicy is null || inheritedPolicy.Kind == MenuAccessPolicyKind.Inherited)
            {
                return false;
            }

            effective = inheritedPolicy;
        }

        return effective.Kind switch
        {
            MenuAccessPolicyKind.Public => true,
            MenuAccessPolicyKind.Permission =>
                effective.Permissions.Count == 1
                && HasPermission(player, effective.Permissions[0]),
            MenuAccessPolicyKind.AnyOf =>
                effective.Permissions.Count > 0
                && effective.Permissions.Any(permission => HasPermission(player, permission)),
            MenuAccessPolicyKind.AllOf =>
                effective.Permissions.Count > 0
                && effective.Permissions.All(permission => HasPermission(player, permission)),
            _ => false,
        };
    }
}
