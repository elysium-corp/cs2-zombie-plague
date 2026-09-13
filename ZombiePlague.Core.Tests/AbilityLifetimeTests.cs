using System.Reflection;
using Common.Effects.Effects.Contracts;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities;
using ZombiePlague.Core.Data.Abilities.Contracts;

namespace ZombiePlague.Core.Tests;

public sealed class AbilityLifetimeTests
{
    [Theory]
    [InlineData("same_pawn", true)]
    [InlineData("disconnected", false)]
    [InlineData("disposed", false)]
    [InlineData("dead", false)]
    [InlineData("reused_slot", false)]
    [InlineData("new_pawn", false)]
    [InlineData("missing_pawn", false)]
    public void DelayedEffectResolvesOnlyItsOriginalSessionAndPawn(string state, bool expected)
    {
        var core = Core();
        var player = Player();
        var pawn = new Mock<CCSPlayerPawn>(MockBehavior.Strict);
        pawn.SetupGet(value => value.IsValid).Returns(true);
        player.SetupGet(value => value.PlayerPawn).Returns(pawn.Object);
        core.Setup(value => value.PlayerManager.GetPlayerFromSessionId(1043)).Returns(player.Object);
        core.Setup(value => value.EntitySystem.GetRefEHandle(pawn.Object)).Returns(new CHandle<CCSPlayerPawn>(500));
        switch (state)
        {
            case "disconnected": player.SetupGet(value => value.IsValid).Returns(false); break;
            case "disposed": player.SetupGet(value => value.PlayerPawn).Throws(new ObjectDisposedException("Player")); break;
            case "dead": player.SetupGet(value => value.IsAlive).Returns(false); break;
            case "reused_slot": player.SetupGet(value => value.SessionId).Returns(1044); break;
            case "new_pawn": core.Setup(value => value.EntitySystem.GetRefEHandle(pawn.Object))
                    .Returns(new CHandle<CCSPlayerPawn>(500 + (1u << 15))); break;
            case "missing_pawn": player.SetupGet(value => value.PlayerPawn).Returns((CCSPlayerPawn?)null); break;
        }

        Assert.Equal(expected, new PlayerPawnReference(1043, 500).TryResolve(core.Object, out var resolved));
        if (expected) Assert.Same(pawn.Object, resolved);
        else Assert.Null(resolved);
    }

    [Fact]
    public void ChargeUnhookWithDisposedTimerAndDisconnectedOwnerStillRemovesInput()
    {
        var core = Core();
        var player = Player();
        var ability = new Charge(core.Object, new ChargeConfig { Enable = true }, null!);
        ability.SetCaster(player.Object);
        using var timer = new CancellationTokenSource();
        timer.Cancel();
        timer.Dispose();
        SetField(ability, "_chargeToken", timer);
        SetField(ability, "_speedBeforeCharge", 280f);
        SetField(ability, "_chargePawn", new PlayerPawnReference(1043, 500));
        player.SetupGet(value => value.IsValid).Returns(false);
        core.Setup(value => value.PlayerManager.GetPlayerFromSessionId(1043)).Returns(player.Object);

        ability.UnHook();
        ability.UnHook();

        Assert.Null(GetField(ability, "_chargeToken"));
        Assert.Null(GetField(ability, "_speedBeforeCharge"));
        Assert.Null(GetField(ability, "_chargePawn"));
        Assert.Single(Mock.Get(core.Object.Event).Invocations, call => call.Method.Name == "remove_OnClientKeyStateChanged");
        player.VerifyGet(value => value.PlayerPawn, Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TrapDisposeCancelsAllTimersAndEffectsEvenWhenEntityRemovalFails(bool despawnFails)
    {
        var core = Core();
        var trap = new TrapEntity(core.Object, new TrapConfig(), Player().Object);
        var entity = new Mock<CParticleSystem>();
        entity.SetupGet(value => value.IsValidEntity).Returns(true);
        if (despawnFails) entity.Setup(value => value.Despawn()).Throws(new InvalidOperationException("Entity removed"));
        typeof(TrapEntity).GetProperty(nameof(TrapEntity.Entity))!.SetValue(trap, entity.Object);
        using var triggerTimer = new CancellationTokenSource();
        using var despawnTimer = new CancellationTokenSource();
        using var freezeTimer = new CancellationTokenSource();
        SetField(trap, "_triggerTask", triggerTimer);
        SetField(trap, "_despawnTask", despawnTimer);
        Action? restore = null;
        core.Setup(value => value.Scheduler.DelayBySeconds(It.IsAny<float>(), It.IsAny<Action>()))
            .Returns((float _, Action callback) => { restore = callback; return freezeTimer; });
        var effect = new Mock<IEffect>();
        var freezes = (List<TrapFreeze>)GetField(trap, "_freezes")!;
        var freeze = new TrapFreeze(core.Object, new(1043, 500), MoveType_t.MOVETYPE_WALK,
            MoveType_t.MOVETYPE_WALK, finished => freezes.Remove(finished)) { Disorientation = effect.Object };
        core.Setup(value => value.PlayerManager.GetPlayerFromSessionId(1043)).Returns((IPlayer?)null);
        freezes.Add(freeze);
        freeze.Schedule(3);

        trap.Dispose();
        trap.Dispose();
        restore!();

        Assert.Null(trap.Entity);
        Assert.True(triggerTimer.IsCancellationRequested);
        Assert.True(despawnTimer.IsCancellationRequested);
        Assert.True(freezeTimer.IsCancellationRequested);
        Assert.Empty(freezes);
        effect.Verify(value => value.Destroy(), Times.Once);
        entity.Verify(value => value.Despawn(), Times.Once);
    }

    [Fact]
    public void TriggeredTrapKeepsFreezeUntilItsTimeoutAndDisposeCannotRestoreTwice()
    {
        var core = Core();
        var trap = new TrapEntity(core.Object, new TrapConfig(), Player().Object);
        var freezes = (List<TrapFreeze>)GetField(trap, "_freezes")!;
        var effect = new Mock<IEffect>();
        Action? restore = null;
        using var timer = new CancellationTokenSource();
        core.Setup(value => value.Scheduler.DelayBySeconds(It.IsAny<float>(), It.IsAny<Action>()))
            .Returns((float _, Action callback) => { restore = callback; return timer; });
        core.Setup(value => value.PlayerManager.GetPlayerFromSessionId(1043)).Returns((IPlayer?)null);
        var freeze = new TrapFreeze(core.Object, new(1043, 500), MoveType_t.MOVETYPE_WALK,
            MoveType_t.MOVETYPE_WALK, finished => freezes.Remove(finished)) { Disorientation = effect.Object };
        freezes.Add(freeze);
        freeze.Schedule(3);

        typeof(TrapEntity).GetMethod("Despawn", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(trap, null);

        Assert.False(timer.IsCancellationRequested);
        Assert.Single(freezes);
        effect.Verify(value => value.Destroy(), Times.Never);
        restore!();
        trap.Dispose();
        Assert.True(timer.IsCancellationRequested);
        Assert.Empty(freezes);
        effect.Verify(value => value.Destroy(), Times.Once);
    }

    [Theory]
    [InlineData("disconnected")]
    [InlineData("unhooked")]
    public void OldKeyCallbackDoesNotAccessMenusOrUseAbility(string state)
    {
        var core = Core();
        var player = Player();
        var ability = new ProbeActive(core.Object, KeyKind.E);
        ability.SetCaster(player.Object);
        if (state == "disconnected") player.SetupGet(value => value.IsValid).Returns(false);
        else ability.UnHook();
        var key = new Mock<IOnClientKeyStateChangedEvent>();
        key.SetupGet(value => value.PlayerId).Returns(30);
        key.SetupGet(value => value.Key).Returns(KeyKind.E);
        key.SetupGet(value => value.Pressed).Returns(true);

        ability.OnClientKeyStateChanged(key.Object);

        Assert.Equal(0, ability.Uses);
        core.VerifyGet(value => value.MenusAPI, Times.Never);
    }

    [Theory]
    [InlineData("disconnected")]
    [InlineData("reused_slot")]
    [InlineData("unhooked")]
    public void MovementCallbackValidatesOwnerBeforeInvokingLeapHandler(string state)
    {
        var core = Core();
        var player = Player();
        var ability = new ProbeActive(core.Object, null);
        ability.SetCaster(player.Object);
        var hook = Mock.Get(core.Object.GameHooks.Movement.RunCommand);
        var callback = (OnRunCommandMovementPreDelegate)hook.Invocations.Single(call => call.Method.Name == "add_Pre").Arguments[0];
        var currentPlayer = Player();
        if (state == "disconnected") player.SetupGet(value => value.IsValid).Returns(false);
        if (state == "reused_slot") currentPlayer.SetupGet(value => value.SessionId).Returns(1044);
        if (state == "unhooked") ability.UnHook();
        var context = new RunCommandMovementPreContext
        {
            Params = new() { Player = currentPlayer.Object, UserCmd = null! }
        };

        callback(ref context);

        Assert.Equal(0, ability.MovementCalls);
    }

    [Fact]
    public void DoubleJumpHooksOnceAndIgnoresAnOldSessionAfterSlotReuse()
    {
        var core = Core();
        var player = Player();
        var ability = new DoubleJump(core.Object, new DoubleJumpConfig { Enable = true });
        ability.SetCaster(player.Object);
        ability.Hook();
        var hook = Mock.Get(core.Object.GameHooks.Movement.SetupMove);
        var subscription = Assert.Single(hook.Invocations, call => call.Method.Name == "add_Pre");
        var callback = (OnSetupMoveMovementPreDelegate)subscription.Arguments[0];
        var replacement = Player();
        replacement.SetupGet(value => value.SessionId).Returns(1044);
        var context = new SetupMoveMovementPreContext
        {
            Params = new() { Player = replacement.Object, UserCmd = null!, MoveData = null! }
        };

        callback(ref context);
        ability.UnHook();
        ability.UnHook();
        callback(ref context);

        Assert.Single(hook.Invocations, call => call.Method.Name == "remove_Pre");
        replacement.VerifyGet(value => value.Pawn, Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void QueuedCooldownFromPreviousUseCannotAdvanceNewCooldown(bool active)
    {
        var core = Core();
        var timers = new List<(Action Callback, CancellationTokenSource Timer)>();
        core.Setup(value => value.Scheduler.RepeatBySeconds(It.IsAny<float>(), It.IsAny<Action>()))
            .Returns((float _, Action callback) =>
            {
                var timer = new CancellationTokenSource();
                timers.Add((callback, timer));
                return timer;
            });
        IAbility ability = active ? new ProbeActive(core.Object, KeyKind.E) : new ProbePassive(core.Object);
        var cooldown = (ICooldownRestricted)ability;
        try
        {
            cooldown.StartCooldown();
            ability.UnHook();
            cooldown.StartCooldown();
            timers[0].Callback();
            Assert.Equal(10, cooldown.RemainingCooldown);
            timers[1].Callback();
            Assert.Equal(9, cooldown.RemainingCooldown);
        }
        finally
        {
            ability.UnHook();
            foreach (var item in timers) item.Timer.Dispose();
        }
    }

    private sealed class ProbeActive(ISwiftlyCore core, KeyKind? key)
        : BaseActiveAbility(core, new ChargeConfig { Enable = true }, null!)
    {
        public int Uses { get; private set; }
        public int MovementCalls { get; private set; }
        public override KeyKind? Key => key;
        public override float Cooldown => 10;
        public override void Use() => Uses++;
        protected override void OnRunCommandHandler(ref RunCommandMovementPreContext context) => MovementCalls++;
    }

    private sealed class ProbePassive(ISwiftlyCore core) : BasePassiveAbility(core, new BlindConfig { Enable = true })
    {
        public override float Cooldown => 10;
    }

    private static Mock<ISwiftlyCore> Core() => new() { DefaultValue = DefaultValue.Mock };

    private static Mock<IPlayer> Player()
    {
        var player = new Mock<IPlayer>(MockBehavior.Strict);
        player.SetupGet(value => value.PlayerID).Returns(30);
        player.SetupGet(value => value.SessionId).Returns(1043);
        player.SetupGet(value => value.IsValid).Returns(true);
        player.SetupGet(value => value.IsAlive).Returns(true);
        player.SetupGet(value => value.IsFakeClient).Returns(false);
        return player;
    }

    private static void SetField(object target, string name, object value) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

    private static object? GetField(object target, string name) =>
        target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target);
}
