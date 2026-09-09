using System.Reflection;
using Common.Hooks;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;
using ZombiePlague.Api.Events;
using ZombiePlague.Api.Events.Contexts.Player;
using ZombiePlague.Core.Api.Events;
using ZombiePlague.Core.Data.Service;
using RoleManager = ZombiePlague.Core.Data.Managers.Contracts.IPlayerManager;

namespace ZombiePlague.Core.Tests;

public sealed class ZombieCosmeticsLifecycleTests
{
    [Theory]
    [InlineData("infected")]
    [InlineData("respawned")]
    [InlineData("nemesis")]
    public void EveryZombieRoleApplicationSchedulesCosmeticsReset(string reason)
    {
        using var fixture = new Fixture();
        switch (reason)
        {
            case "infected":
                var infection = new PlayerInfectedContext(fixture.Player, null);
                fixture.Hooks.Dispatch(ref infection);
                break;
            case "respawned":
                fixture.ApplyRole();
                break;
            case "nemesis":
                var nemesis = new PlayerBecameNemesisContext(fixture.Player);
                fixture.Hooks.Dispatch(ref nemesis);
                break;
        }
        Assert.Single(fixture.Updates);
    }

    [Theory]
    [InlineData("human")]
    [InlineData("dead")]
    [InlineData("disconnected")]
    [InlineData("new_pawn")]
    [InlineData("unloaded")]
    [InlineData("restarted")]
    public void PendingResetDoesNotModifyAnotherLifeOrHuman(string change)
    {
        using var fixture = new Fixture();
        fixture.ApplyRole();
        Assert.Single(fixture.Updates);
        switch (change)
        {
            case "human": fixture.Zombie = false; break;
            case "dead": fixture.Alive = false; break;
            case "disconnected": fixture.Connected = false; break;
            case "new_pawn": fixture.PawnAddress++; break;
            case "unloaded": fixture.Service.Unregister(); break;
            case "restarted": fixture.Service.Unregister(); fixture.Service.Register(); break;
        }
        fixture.Updates.Dequeue()();
        Assert.Empty(fixture.Updates);
    }

    [Fact]
    public void HumanRoleAndStoppedServiceDoNotScheduleReset()
    {
        using var fixture = new Fixture { Zombie = false };
        fixture.ApplyRole();
        Assert.Empty(fixture.Updates);
        fixture.Service.Unregister();
        fixture.Zombie = true;
        fixture.ApplyRole();
        Assert.Empty(fixture.Updates);
    }

    private sealed class Fixture : IDisposable
    {
        public bool Zombie { get; set; } = true;
        public bool Alive { get; set; } = true;
        public bool Connected { get; set; } = true;
        public nint PawnAddress { get; set; } = 100;
        public Queue<Action> Updates { get; } = new();
        public HookService Hooks { get; } = new();
        public IPlayer Player { get; }
        public InfectionService Service { get; }

        public Fixture()
        {
            var pawn = new Mock<CCSPlayerPawn>(MockBehavior.Strict);
            pawn.Setup(value => value.IsValid).Returns(true);
            pawn.Setup(value => value.Address).Returns(() => PawnAddress);
            Player = Stub<IPlayer>((method, _) => method.Name switch
            {
                "get_IsValid" => true,
                "get_IsAlive" => Alive,
                "get_SessionId" => 7UL,
                "get_PlayerPawn" => pawn.Object,
                _ => throw new InvalidOperationException(method.Name)
            });
            var core = Stub<ISwiftlyCore>((method, _) => method.Name switch
            {
                "get_PlayerManager" => Stub(method.ReturnType, (_, args) =>
                {
                    Assert.Equal(7UL, args![0]);
                    return Connected ? Player : null;
                }),
                "get_Scheduler" => Stub(method.ReturnType, (_, args) =>
                {
                    Updates.Enqueue((Action)args![0]!);
                    return null;
                }),
                "get_GameHooks" => Stub(method.ReturnType, NoopHooks),
                _ => throw new InvalidOperationException(method.Name)
            });
            var roles = Stub<RoleManager>((method, _) => method.Name == "IsZombie"
                ? Zombie : throw new InvalidOperationException(method.Name));
            var players = new ZombiePlaguePlayerEvents(Hooks);
            var events = Stub<IZombiePlagueEvents>((method, _) => method.Name == "get_Players"
                ? players : throw new InvalidOperationException(method.Name));
            Service = new(core, roles, events);
            Service.Register();
        }

        public void ApplyRole()
        {
            var context = new PlayerRoleAppliedContext(Player);
            Hooks.Dispatch(ref context);
        }

        public void Dispose() => Service.Unregister();
    }

    private static object? NoopHooks(MethodInfo method, object?[]? _) =>
        method.Name.StartsWith("get_", StringComparison.Ordinal)
            ? Stub(method.ReturnType, NoopHooks) : null;

    private static T Stub<T>(Func<MethodInfo, object?[]?, object?> handler) where T : class =>
        (T)Stub(typeof(T), handler);

    private static object Stub(Type type, Func<MethodInfo, object?[]?, object?> handler)
    {
        var proxy = DispatchProxy.Create(type, typeof(InterfaceStub));
        ((InterfaceStub)proxy).Handler = handler;
        return proxy;
    }

    public class InterfaceStub : DispatchProxy
    {
        public Func<MethodInfo, object?[]?, object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!, args);
    }
}
