using System.Reflection;
using Shop.Api.Data;
using Shop.Core.Application;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Tests;

public sealed class ShopPurchaseCounterTests
{
    [Fact]
    public void ShopQuotaIsSharedAcrossOffersButIsolatedByPlayerAndSide()
    {
        var counter = new ShopPurchaseCounter();
        var player = Player(100);
        var other = Player(200);
        var offer = new ShopOffer(1, ShopType.Human, "builtin", "armor", "Armor", null,
            300, null, 0, 1, 1, 60, ShopAccessMode.Everyone, new HashSet<string>(), true, 0);
        counter.Record(player, offer);
        counter.Record(player, offer with { Id = 2 });
        Assert.Equal(2, counter.RoundCount(player, ShopType.Human));
        Assert.Equal(0, counter.RoundCount(player, ShopType.Zombie));
        Assert.Equal(0, counter.RoundCount(other, ShopType.Human));
        Assert.Equal(ShopAvailabilityReason.RoundLimitReached, counter.Evaluate(player, offer).Reason);
        Assert.InRange(counter.RemainingCooldown(player, offer).TotalSeconds, 55, 60);
        Assert.Equal(TimeSpan.Zero, counter.RemainingCooldown(other, offer));
        counter.ResetRound();
        Assert.Equal(0, counter.RoundCount(player, ShopType.Human));
        Assert.Equal(ShopAvailabilityReason.MapLimitReached, counter.Evaluate(player, offer).Reason);
        Assert.Equal(ShopAvailabilityReason.CooldownActive, counter.Evaluate(player, offer with { MaxPurchasesPerMap = 0 }).Reason);
        counter.ResetMap();
        Assert.True(counter.Evaluate(player, offer).Allowed);
        Assert.Equal(TimeSpan.Zero, counter.RemainingCooldown(player, offer));
    }

    private static IPlayer Player(ulong steamId)
    {
        var player = DispatchProxy.Create<IPlayer, ShopInputTests.InterfaceStub>();
        ((ShopInputTests.InterfaceStub)(object)player).Handler = (method, _) => method.Name switch
        {
            "get_IsAuthorized" => true, "get_SteamID" => steamId,
            _ => throw new InvalidOperationException(method.Name)
        };
        return player;
    }
}
