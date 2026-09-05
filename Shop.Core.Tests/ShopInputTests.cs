using System.Reflection;
using CustomEquipment.Api;
using Microsoft.Extensions.Logging.Abstractions;
using Shop.Core.Application;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Tests;

public sealed class ShopInputTests
{
    [Fact]
    public void OpenMenuBlocksAmmoBeforeEquipmentOrMoneyIsAccessedAndClosingRestoresInput()
    {
        IMenuAPI? currentMenu = Stub<IMenuAPI>((_, _) => throw new InvalidOperationException());
        var menus = Stub<IMenuManagerAPI>((method, _) => method.Name == "GetCurrentMenu"
            ? currentMenu : throw new InvalidOperationException(method.Name));
        var core = Stub<ISwiftlyCore>((method, _) => method.Name == "get_MenusAPI"
            ? menus : throw new InvalidOperationException(method.Name));
        var player = Stub<IPlayer>((method, _) => method.Name is "get_IsValid" or "get_IsAlive"
            ? true : throw new InvalidOperationException(method.Name));
        var weaponLookups = 0;
        var equipment = Stub<ICustomEquipmentApi>((method, arguments) =>
        {
            Assert.Equal("TryGetActiveWeapon", method.Name);
            weaponLookups++;
            arguments![1] = null;
            return false;
        });
        var service = new ShopPurchaseService(
            core, null!, null!, null!, null!,
            () => throw new InvalidOperationException("Денежные операции не ожидались."),
            () => equipment, null!,
            () => throw new InvalidOperationException("Сообщения в чат не ожидались."),
            NullLogger<ShopPurchaseService>.Instance,
            null!);

        Assert.False(service.TryPurchaseActiveWeaponAmmo(player));
        Assert.Equal(0, weaponLookups);

        currentMenu = null;
        Assert.False(service.TryPurchaseActiveWeaponAmmo(player));
        Assert.Equal(1, weaponLookups);
    }

    private static T Stub<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class
    {
        var proxy = DispatchProxy.Create<T, InterfaceStub>();
        ((InterfaceStub)(object)proxy).Handler = handler;
        return proxy;
    }

    public class InterfaceStub : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) =>
            Handler(targetMethod!, args);
    }
}
