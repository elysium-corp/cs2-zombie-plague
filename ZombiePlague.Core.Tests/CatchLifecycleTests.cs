using System.Reflection;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Config.Zombie;
using ZombiePlague.Core.Data.Abilities;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Data.Entities.Zombie;
using ZombiePlague.Core.Data.Entities.Zombie.Classes;

namespace ZombiePlague.Core.Tests;

public sealed class CatchLifecycleTests
{
    [Fact]
    public void DisconnectedTargetDoesNotInterruptRoleCleanup()
    {
        using var fixture = new Fixture();
        fixture.Target.SetupGet(player => player.IsValid).Returns(false);
        fixture.Target.SetupGet(player => player.PlayerPawn)
            .Throws(new ObjectDisposedException("Player slot=30, sessionId=1043"));
        var otherAbility = new Mock<IAbility>();
        var zombieClass = new ZCatalogClass(new ZombieSmoker(), [fixture.Ability, otherAbility.Object]);
        var zombie = Zombie.Create(fixture.Core.Object, fixture.Caster.Object, zombieClass);

        zombie.Unbind();
        zombie.Unbind();

        fixture.AssertCleaned();
        fixture.Target.VerifyGet(player => player.PlayerPawn, Times.Never);
        otherAbility.Verify(ability => ability.UnHook(), Times.Exactly(2));
    }

    [Fact]
    public void DisposalDuringPawnLookupStillCancelsTimerAndRemovesBeam()
    {
        using var fixture = new Fixture();
        fixture.Target.SetupGet(player => player.PlayerPawn).Throws(new ObjectDisposedException("Player"));
        fixture.Ability.UnHook();
        fixture.AssertCleaned();
    }

    [Fact]
    public void RespawnedTargetIsNotRestoredUsingPreviousPawnMovement()
    {
        using var fixture = new Fixture();
        fixture.Core.Setup(core => core.EntitySystem.GetRefEHandle(fixture.Pawn.Object))
            .Returns(new CHandle<CCSPlayerPawn>(500 + (1u << 15)));

        fixture.Ability.UnHook();

        fixture.AssertCleaned();
        fixture.Pawn.Verify(pawn => pawn.MoveTypeUpdated(), Times.Never);
    }

    [Theory]
    [InlineData("dead")]
    [InlineData("missing_pawn")]
    [InlineData("invalid_pawn")]
    public void MissingOrDeadTargetDoesNotPreventCleanup(string state)
    {
        using var fixture = new Fixture();
        switch (state)
        {
            case "dead": fixture.Target.SetupGet(player => player.IsAlive).Returns(false); break;
            case "missing_pawn": fixture.Target.SetupGet(player => player.PlayerPawn).Returns((CCSPlayerPawn?)null); break;
            case "invalid_pawn": fixture.Pawn.SetupGet(pawn => pawn.IsValid).Returns(false); break;
        }

        fixture.Ability.UnHook();

        fixture.AssertCleaned();
    }

    [Fact]
    public void DisposedTimerDoesNotPreventUnsubscription()
    {
        using var fixture = new Fixture();
        fixture.Target.SetupGet(player => player.IsValid).Returns(false);
        fixture.Timer.Cancel();
        fixture.Timer.Dispose();

        fixture.Ability.UnHook();

        fixture.AssertCleaned();
    }

    [Fact]
    public void UnexpectedBeamErrorStillRemovesInputHookAndAllStoredState()
    {
        using var fixture = new Fixture();
        fixture.Target.SetupGet(player => player.IsValid).Returns(false);
        fixture.Beam.Setup(beam => beam.Despawn()).Throws(new InvalidOperationException("Beam failure"));

        Assert.Throws<InvalidOperationException>(fixture.Ability.UnHook);
        fixture.Ability.UnHook();

        fixture.AssertCleaned();
    }

    [Fact]
    public void CancelledCallbackCannotCancelANewerCatch()
    {
        using var fixture = new Fixture();
        fixture.Target.SetupGet(player => player.IsValid).Returns(false);
        fixture.Ability.UnHook();
        var timers = new List<(Action Callback, CancellationTokenSource Source)>();
        fixture.Core.Setup(core => core.Scheduler.RepeatBySeconds(It.IsAny<float>(), It.IsAny<Action>()))
            .Returns((float _, Action callback) =>
            {
                var source = new CancellationTokenSource();
                timers.Add((callback, source));
                return source;
            });
        try
        {
            Invoke(fixture.Ability, "CreateCatchingHandler");
            fixture.Ability.UnHook();
            Invoke(fixture.Ability, "CreateCatchingHandler");

            timers[0].Callback();

            Assert.True(timers[0].Source.IsCancellationRequested);
            Assert.False(timers[1].Source.IsCancellationRequested);
            fixture.Ability.UnHook();
            Assert.True(timers[1].Source.IsCancellationRequested);
        }
        finally
        {
            foreach (var timer in timers) timer.Source.Dispose();
        }
    }

    private sealed class Fixture : IDisposable
    {
        public Mock<ISwiftlyCore> Core { get; } = new() { DefaultValue = DefaultValue.Mock };
        public Mock<IPlayer> Caster { get; } = new();
        public Mock<IPlayer> Target { get; } = new(MockBehavior.Strict);
        public Mock<CCSPlayerPawn> Pawn { get; } = new(MockBehavior.Strict);
        public Mock<CBeam> Beam { get; } = new(MockBehavior.Strict);
        public CancellationTokenSource Timer { get; } = new();
        public Catch Ability { get; }
        private readonly CancellationToken _token;

        public Fixture()
        {
            _token = Timer.Token;
            Target.SetupGet(player => player.IsValid).Returns(true);
            Target.SetupGet(player => player.IsAlive).Returns(true);
            Target.SetupGet(player => player.PlayerPawn).Returns(Pawn.Object);
            Pawn.SetupGet(pawn => pawn.IsValid).Returns(true);
            Beam.SetupGet(beam => beam.IsValidEntity).Returns(true);
            Beam.Setup(beam => beam.Despawn());
            Ability = new Catch(Core.Object, new CatchConfig { Enable = true }, () => throw new InvalidOperationException());
            Ability.SetCaster(Caster.Object);
            Ability.IsActive = true;

            // Снимок уже активного захвата до disconnect: геометрический trace
            // и нативные ref-поля движения требуют запущенного CS2.
            typeof(BaseActiveAbility).GetProperty("Target", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(Ability, Target.Object);
            SetField(Ability, "_catchToken", Timer);
            SetField(Ability, "_catchBeam", Beam.Object);
            SetField(Ability, "_targetMoveType", MoveType_t.MOVETYPE_WALK);
            SetField(Ability, "_targetActualMoveType", MoveType_t.MOVETYPE_WALK);
            SetField(Ability, "_targetPawnHandle", 500u);
        }

        public void AssertCleaned()
        {
            Assert.True(_token.IsCancellationRequested);
            Assert.False(Ability.IsActive);
            Assert.Null(typeof(BaseActiveAbility).GetProperty("Target", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(Ability));
            foreach (var name in new[] { "_catchToken", "_catchBeam", "_targetMoveType", "_targetActualMoveType", "_targetPawnHandle" })
                Assert.Null(typeof(Catch).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Ability));
            var subscriptions = Mock.Get(Core.Object.Event).Invocations;
            Assert.Single(subscriptions, call => call.Method.Name == "add_OnClientKeyStateChanged");
            Assert.Single(subscriptions, call => call.Method.Name == "remove_OnClientKeyStateChanged");
            Beam.Verify(beam => beam.Despawn(), Times.Once);
        }

        public void Dispose() => Timer.Dispose();
    }

    private static void SetField(Catch ability, string name, object value) =>
        typeof(Catch).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(ability, value);

    private static void Invoke(Catch ability, string name) =>
        typeof(Catch).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(ability, null);
}
