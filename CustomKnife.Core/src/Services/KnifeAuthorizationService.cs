using Admin.Api;
using CustomKnife.Data.Knives;
using CustomKnife.Data.Models;
using SwiftlyS2.Shared.Players;

namespace CustomKnife.Services;

internal sealed class KnifeAuthorizationService(IAdminApi adminApi) : IKnifeAuthorizationService
{
    public bool CanUse(IPlayer player, IKnife knife)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(knife);

        var permission = GetRequiredPermission(knife);

        return knife.InternalName == KnifeDefaults.DefaultKnifeId ||
               permission is null ||
               adminApi.HasPermission(player, permission);
    }

    public string? GetRequiredPermission(IKnife knife)
    {
        ArgumentNullException.ThrowIfNull(knife);

        return knife is IAccessControlledKnife accessControlled
            ? accessControlled.RequiredPermission
            : null;
    }
}
