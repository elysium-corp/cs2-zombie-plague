using System.Reflection;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using Xunit;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;

namespace ZombiePlague.Core.Tests;

public sealed class AbilityMenuInputTests
{
    [Fact]
    public void MenuSelectionDoesNotUseAbilityOrNotifyCooldownAndClosingRestoresInput()
    {
        IMenuAPI? currentMenu = Stub<IMenuAPI>(_ => throw new InvalidOperationException());
        var menus = Stub<IMenuManagerAPI>(method => method.Name == "GetCurrentMenu"
            ? currentMenu : throw new InvalidOperationException(method.Name));
        var core = Stub<ISwiftlyCore>(method => method.Name == "get_MenusAPI"
            ? menus : throw new InvalidOperationException(method.Name));
        var player = Stub<IPlayer>(method => method.Name == "get_PlayerID"
            ? 7 : throw new InvalidOperationException(method.Name));
        var keyEvent = Stub<IOnClientKeyStateChangedEvent>(method => method.Name switch
        {
            "get_PlayerId" => 7,
            "get_Pressed" => true,
            "get_Key" => KeyKind.E,
            _ => throw new InvalidOperationException(method.Name)
        });
        var ability = new ProbeAbility(core);
        ability.SetCaster(player);

        ability.OnClientKeyStateChanged(keyEvent);
        Assert.Equal(0, ability.Uses);
        ability.IsActive = true;
        ability.OnClientKeyStateChanged(keyEvent);
        Assert.Equal(0, ability.Uses);

        ability.IsActive = false;
        currentMenu = null;
        ability.OnClientKeyStateChanged(keyEvent);
        Assert.Equal(1, ability.Uses);
    }

    private sealed class ProbeAbility(ISwiftlyCore core) : BaseActiveAbility(
        core, new AbilityConfig(),
        () => throw new InvalidOperationException("Уведомление о перезарядке в меню не ожидалось."))
    {
        public int Uses { get; private set; }
        public override KeyKind? Key => KeyKind.E;
        public override float Cooldown => 10;
        public override void Hook() { }
        public override void Use() => Uses++;
    }

    private sealed class AbilityConfig : IAbilityConfig
    {
        public bool Enable { get; set; } = true;
    }

    private static T Stub<T>(Func<MethodInfo, object?> handler) where T : class
    {
        var proxy = DispatchProxy.Create<T, InterfaceStub>();
        ((InterfaceStub)(object)proxy).Handler = handler;
        return proxy;
    }

    public class InterfaceStub : DispatchProxy
    {
        public Func<MethodInfo, object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!);
    }
}
