using System.Reflection;
using Common.Hooks;
using CustomEquipment.Api;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using Economy.Api;
using Microsoft.Extensions.Logging.Abstractions;
using Shop.Api.Data;
using Shop.Api.Events;
using Shop.Core.Application;
using Shop.Core.Data;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Tests;

public sealed class ShopAmmoSoundTests
{
    [Fact]
    public void SuccessfulRefillPlaysPurchaseSoundAfterAmmoAndPaymentAreCommitted()
    {
        var fixture = new PurchaseFixture();

        Assert.True(fixture.Service.TryPurchaseActiveWeaponAmmo(fixture.Player));

        Assert.Equal(90, fixture.Balance);
        Assert.Equal(1, fixture.RefillCalls);
        Assert.Equal(1, fixture.PurchasedEvents);
        Assert.Equal(["purchased"], fixture.Feedback.Played);
        Assert.Empty(fixture.Rejections);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(0)]
    public void FullReservePlaysCancelSoundWithoutChargingOrRefilling(int balance)
    {
        var fixture = new PurchaseFixture
        {
            ReserveFull = true,
            Balance = balance,
            AmmoAvailability = balance == 0
                ? ShopAvailability.Rejected(ShopAvailabilityReason.InsufficientFunds)
                : ShopAvailability.Available()
        };

        Assert.False(fixture.Service.TryPurchaseActiveWeaponAmmo(fixture.Player));

        Assert.Equal(balance, fixture.Balance);
        Assert.Equal(0, fixture.RefillCalls);
        Assert.Equal(0, fixture.PurchasedEvents);
        Assert.Equal(["full"], fixture.Feedback.Played);
        Assert.Equal([ShopAvailabilityReason.AmmoFull], fixture.Rejections);
    }

    [Fact]
    public void RejectedRefillRefundsMoneyAndNeverPlaysPurchaseSound()
    {
        var fixture = new PurchaseFixture { RefillSucceeds = false };

        Assert.False(fixture.Service.TryPurchaseActiveWeaponAmmo(fixture.Player));

        Assert.Equal(100, fixture.Balance);
        Assert.Equal(1, fixture.RefillCalls);
        Assert.Equal(0, fixture.PurchasedEvents);
        Assert.Equal(["full"], fixture.Feedback.Played);
        Assert.Equal([ShopAvailabilityReason.AmmoFull], fixture.Rejections);
    }

    [Fact]
    public void InsufficientFundsDoNotPlayPurchaseOrFullReserveSound()
    {
        var fixture = new PurchaseFixture
        {
            Balance = 0,
            AmmoAvailability = ShopAvailability.Rejected(ShopAvailabilityReason.InsufficientFunds)
        };

        Assert.False(fixture.Service.TryPurchaseActiveWeaponAmmo(fixture.Player));

        Assert.Equal(0, fixture.Balance);
        Assert.Equal(0, fixture.RefillCalls);
        Assert.Equal(0, fixture.PurchasedEvents);
        Assert.Empty(fixture.Feedback.Played);
        Assert.Equal([ShopAvailabilityReason.InsufficientFunds], fixture.Rejections);
    }

    private sealed class PurchaseFixture
    {
        internal bool ReserveFull { get; init; }
        internal bool RefillSucceeds { get; init; } = true;
        internal ShopAvailability AmmoAvailability { get; init; } = ShopAvailability.Available();
        internal int Balance { get; set; } = 100;
        internal int RefillCalls { get; private set; }
        internal int PurchasedEvents { get; private set; }
        internal List<ShopAvailabilityReason> Rejections { get; } = [];
        internal RecordingFeedback Feedback { get; } = new();
        internal IPlayer Player { get; }
        internal ShopPurchaseService Service { get; }

        internal PurchaseFixture()
        {
            Player = Stub<IPlayer>((method, _) => method.Name switch
            {
                "get_IsValid" or "get_IsAlive" => true,
                "get_IsAuthorized" => false,
                "get_PlayerID" => 7,
                _ => throw new InvalidOperationException(method.Name)
            });
            var menus = Stub<IMenuManagerAPI>((method, _) => method.Name == "GetCurrentMenu"
                ? null : throw new InvalidOperationException(method.Name));
            var core = Stub<ISwiftlyCore>((method, _) => method.Name == "get_MenusAPI"
                ? menus : throw new InvalidOperationException(method.Name));
            var weapon = Stub<IWeapon>((method, _) => method.Name == "get_InternalName"
                ? "test_weapon" : throw new InvalidOperationException(method.Name));
            var equipment = Stub<ICustomEquipmentApi>((method, arguments) =>
            {
                switch (method.Name)
                {
                    case "TryGetActiveWeapon":
                    case "TryGetRegisteredItem":
                        arguments![1] = weapon;
                        return true;
                    case "CanUseItem":
                        return true;
                    case "CanRefillActiveWeapon":
                        return !ReserveFull;
                    case "TryRefillActiveWeapon":
                        Assert.Equal(90, Balance);
                        Assert.Empty(Feedback.Played);
                        RefillCalls++;
                        arguments![3] = new AmmoRefillResult(1, 100);
                        return RefillSucceeds;
                    default:
                        throw new InvalidOperationException(method.Name);
                }
            });
            var economy = Stub<IEconomyApi>((method, arguments) =>
            {
                switch (method.Name)
                {
                    case "GetBalance":
                        return Balance;
                    case "HasEnoughMoney":
                        return Balance >= (int)arguments![1]!;
                    case "TrySpendMoney":
                        Balance -= (int)arguments![1]!;
                        return true;
                    case "GiveMoney":
                        Balance += (int)arguments![1]!;
                        return null;
                    default:
                        throw new InvalidOperationException(method.Name);
                }
            });
            var offer = new ShopOffer(
                1, ShopType.Human, "custom_equipment", "test_weapon", "Equipment.Test.Name",
                null, 50, 10, 1, 0, 0, 0, ShopAccessMode.Everyone, new HashSet<string>(), true, 0);
            var cache = new ShopSnapshotCache();
            cache.Replace(new ShopSnapshot(
                new Dictionary<ShopType, ShopStorefrontDefinition>
                {
                    [ShopType.Human] = new(ShopType.Human, "Shop.Human.Title", true, ShopSortMode.Priority)
                },
                [], [new ShopOfferDefinition(offer, null, "{}")], "test", DateTimeOffset.UtcNow));
            var access = new TestAccess(() => AmmoAvailability);
            var hooks = new HookService();
            hooks.Hook<ShopAmmoPurchasedContext>((ref ShopAmmoPurchasedContext context) => PurchasedEvents++);
            hooks.Hook<ShopPurchaseRejectedContext>((ref ShopPurchaseRejectedContext context) => Rejections.Add(context.Reason));
            Service = new ShopPurchaseService(
                core, cache, access, null!, null!, () => economy, () => equipment, hooks,
                () => throw new InvalidOperationException("Сообщения в чат не ожидались."),
                NullLogger<ShopPurchaseService>.Instance, Feedback);
        }
    }

    private sealed class RecordingFeedback : IShopSoundFeedback
    {
        internal List<string> Played { get; } = [];

        public void AmmoPurchased(IPlayer player) => Played.Add("purchased");
        public void AmmoFull(IPlayer player) => Played.Add("full");
    }

    private sealed class TestAccess(Func<ShopAvailability> availability) : IShopAccessEvaluator
    {
        public ShopType GetShopType(IPlayer player) => ShopType.Human;

        public ShopAvailability Evaluate(IPlayer player, ShopOfferDefinition offer, int? price = null) =>
            throw new InvalidOperationException("Покупка самого оружия не ожидалась.");

        public ShopAvailability EvaluateAmmo(IPlayer player, ShopOfferDefinition offer) => availability();
    }

    private static T Stub<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        var proxy = DispatchProxy.Create<T, ShopInputTests.InterfaceStub>();
        ((ShopInputTests.InterfaceStub)(object)proxy).Handler = handler;
        return proxy;
    }
}
