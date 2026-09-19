using System.Reflection;
using CustomHud.Api;
using Shop.Core.Application;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Tests;

public sealed class ShopAmmoHintTests
{
    [Theory]
    [InlineData(0, 0, true)]
    [InlineData(0, 90, false)]
    [InlineData(30, 0, false)]
    [InlineData(-1, 0, false)]
    public void OnlyAnEmptyMagazineAndReserveRequireBuying(int clip, int reserve, bool expected) =>
        Assert.Equal(expected, ShopAmmoHintService.IsEmpty(clip, reserve));

    [Fact]
    public void EmptyWeaponPublishesOnceAndRefillAllowsTheNextEpisode()
    {
        var fixture = new Fixture();
        var player = Player(1, 11);
        var hint = new ShopAmmoHintService.Hint(100, "ak47", 100);
        fixture.Service.Update(player, hint);
        fixture.Service.Update(player, hint);
        Assert.Equal(["publish:1"], fixture.Calls);
        fixture.Service.Update(player, null);
        fixture.Service.Update(player, null);
        fixture.Service.Update(player, hint);
        Assert.Equal(["publish:1", "hide:1", "publish:1"], fixture.Calls);
    }

    [Fact]
    public void SwitchingEmptyWeaponsCancelsThePreviousPendingBanner()
    {
        var fixture = new Fixture();
        var player = Player(1, 11);
        fixture.Service.Update(player, new(100, "ak47", 100));
        fixture.Service.Update(player, new(200, "deagle", 20));
        Assert.Equal(["publish:1", "hide:1", "publish:1"], fixture.Calls);
    }

    [Fact]
    public void RejectedPublicationRetriesAndSlotReuseDoesNotHideTheNewSession()
    {
        var fixture = new Fixture { Accept = false };
        var hint = new ShopAmmoHintService.Hint(100, "ak47", 100);
        fixture.Service.Update(Player(1, 11), hint);
        fixture.Accept = true;
        fixture.Service.Update(Player(1, 11), hint);
        fixture.Service.Update(Player(1, 22), hint);
        Assert.Equal(["publish:1", "publish:1", "publish:1"], fixture.Calls);
    }

    [Fact]
    public void OnePlayersPurchaseDoesNotClearOtherPlayersAndConfigurationRearmsHint()
    {
        var fixture = new Fixture();
        var hint = new ShopAmmoHintService.Hint(100, "ak47", 100);
        fixture.Service.Update(Player(1, 11), hint);
        fixture.Service.Update(Player(2, 22), hint);
        fixture.Service.Hide(Player(1, 11));
        fixture.Service.Update(Player(2, 22), hint);
        Assert.Equal(["publish:1", "publish:2", "hide:1"], fixture.Calls);
        fixture.Service.Reset();
        fixture.Service.Update(Player(2, 22), hint);
        Assert.Equal(["publish:1", "publish:2", "hide:1", "clear", "publish:2"], fixture.Calls);
    }

    private sealed class Fixture
    {
        internal bool Accept { get; set; } = true;
        internal List<string> Calls { get; } = [];
        internal ShopAmmoHintService Service { get; }

        internal Fixture()
        {
            var client = new BannerNotificationClient();
            client.Bind(Stub<IBannerNotificationApi>((method, arguments) =>
            {
                if (method.Name == "Clear") { Calls.Add("clear"); return null; }
                Assert.Equal(ShopAmmoHintService.EventKey, arguments![1]);
                var player = (IPlayer)arguments[0]!;
                Calls.Add(method.Name.ToLowerInvariant() + ":" + player.PlayerID);
                return method.Name == "Publish" ? Accept : null;
            }));
            Service = new ShopAmmoHintService(null!, null!, null!, null!, client, null!);
        }
    }

    private static IPlayer Player(int id, ulong session) => Stub<IPlayer>((method, _) => method.Name switch
    {
        "get_PlayerID" => id,
        "get_SessionId" => session,
        _ => throw new InvalidOperationException(method.Name)
    });

    private static T Stub<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        var proxy = DispatchProxy.Create<T, ShopInputTests.InterfaceStub>();
        ((ShopInputTests.InterfaceStub)(object)proxy).Handler = handler;
        return proxy;
    }
}
