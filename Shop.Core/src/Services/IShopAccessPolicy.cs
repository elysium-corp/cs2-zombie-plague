using SwiftlyS2.Shared.Players;

namespace Shop.Core.Services;

internal interface IShopAccessPolicy
{
    bool CanUse(IPlayer player);
}
