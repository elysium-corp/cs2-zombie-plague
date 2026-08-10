using Microsoft.Extensions.Options;
using Shop.Core.Data.Configs;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;

namespace Shop.Core.Services;

internal sealed class ShopAccessPolicy(
    IZombiePlagueApi zombiePlagueApi,
    IOptionsMonitor<ShopConfig> configMonitor
) : IShopAccessPolicy
{
    public bool CanUse(IPlayer player)
    {
        return configMonitor.CurrentValue.Enabled
               && player.IsValid
               && player.IsAlive
               && player.PlayerPawn is { IsValid: true }
               && player.Controller.Team == Team.CT
               && !zombiePlagueApi.IsInfected(player);
    }
}
