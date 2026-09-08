using System.Reflection;
using CustomKnife.Data.Services;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;
using ZombiePlague.Api;

namespace CustomKnife.Core.Tests;

public sealed class KnifeGrantLifecycleTests
{
    [Theory]
    [InlineData("infected")]
    [InlineData("dead")]
    [InlineData("disconnected")]
    [InlineData("respawned")]
    [InlineData("unloaded")]
    public void PendingHumanKnifeCannotBeGivenAfterPlayerStateChanges(string change)
    {
        using var fixture = new Fixture();
        Assert.True(fixture.Service.TryGiveKnife(fixture.Player));
        Assert.Single(fixture.Updates);

        switch (change)
        {
            case "infected": fixture.Infected = true; break;
            case "dead": fixture.Alive = false; break;
            case "disconnected": fixture.Connected = false; break;
            case "respawned": fixture.PawnAddress++; break;
            case "unloaded": fixture.Service.Dispose(); break;
        }

        // Зависимости выдачи намеренно отсутствуют: устаревший callback не должен их вызывать.
        fixture.Updates.Dequeue()();
        Assert.Empty(fixture.Updates);
    }

    [Fact]
    public void RespawningZombieNeverQueuesHumanKnife()
    {
        using var fixture = new Fixture { Infected = true };
        Assert.False(fixture.Service.TryGiveKnife(fixture.Player));
        Assert.Empty(fixture.Updates);
    }

    private sealed class Fixture : IDisposable
    {
        public bool Infected { get; set; }
        public bool Alive { get; set; } = true;
        public bool Connected { get; set; } = true;
        public nint PawnAddress { get; set; } = 100;
        public Queue<Action> Updates { get; } = new();
        public IPlayer Player { get; }
        public KnifeService Service { get; }

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
                "get_PlayerPawn" or "get_RequiredPlayerPawn" => pawn.Object,
                _ => throw new InvalidOperationException(method.Name)
            });
            var core = Stub<ISwiftlyCore>((method, _) => method.Name switch
            {
                "get_PlayerManager" => Stub(method.ReturnType, (member, args) =>
                {
                    Assert.Equal("GetPlayerFromSessionId", member.Name);
                    Assert.Equal(7UL, args![0]);
                    return Connected ? Player : null;
                }),
                "get_Scheduler" => Stub(method.ReturnType, (member, args) =>
                {
                    Assert.Equal("NextWorldUpdate", member.Name);
                    Updates.Enqueue((Action)args![0]!);
                    return null;
                }),
                _ => throw new InvalidOperationException(method.Name)
            });
            var zombies = Stub<IZombiePlagueApi>((method, _) => method.Name == "IsInfected"
                ? Infected : throw new InvalidOperationException(method.Name));
            Service = new(core, null!, null!, zombies, null!);
        }

        public void Dispose() => Service.Dispose();
    }

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
