using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Common.Hooks.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using Xunit;
using ZombiePlague.Api.Data.Rounds;
using ZombiePlague.Core.Config.Core;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Entities;
using ZombiePlague.Core.Data.Entities.Human;
using ZombiePlague.Core.Data.Entities.Zombie;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Data.Rounds.Registrator;
using ZombiePlague.Core.Data.Service;
using PlayerRegistryContract = ZombiePlague.Core.Data.Managers.Contracts.IPlayerManager;

namespace ZombiePlague.Core.Tests;

public sealed class IdleRoundPreparationTests
{
    [Fact]
    public void EmptyServerKeepsPreparationUntilTheFirstAlivePlayerReceivesAFullCountdown()
    {
        using var f = new Fixture();
        for (var i = 0; i < 100; i++) f.Tick();
        Assert.True(f.Manager.IsPreparing);
        Assert.False(f.Timer.IsCancellationRequested);
        Assert.Null(f.Manager.CurrentRound);
        Assert.Equal(0, f.Factory.Calls);

        f.Join(alive: true);
        f.Tick(); f.Tick();
        Assert.True(f.Manager.IsPreparing);
        Assert.Null(f.Manager.CurrentRound);
        f.Tick();

        Assert.Same(f.Round, f.Manager.CurrentRound);
        Assert.False(f.Manager.IsPreparing);
        Assert.Equal(1, f.Round.Starts);
    }

    [Fact]
    public void DeadFirstPlayerCanRespawnDuringTheIdlePreparation()
    {
        using var f = new Fixture();
        for (var i = 0; i < 100; i++) f.Tick();
        f.Join(alive: false);
        f.Tick();

        Assert.True(f.Manager.TryRespawnPlayer(f.Player.Object));
        Assert.Equal(1, f.Players.Respawns);
        Assert.True(f.Player.Object.IsAlive);
        f.Tick(); f.Tick(); f.Tick();
        Assert.Same(f.Round, f.Manager.CurrentRound);
    }

    [Fact]
    public void DisconnectDuringPreparationRestoresTheFullCountdownForTheNextPlayer()
    {
        using var f = new Fixture();
        f.Join(alive: true);
        f.Tick(); f.Tick();
        f.Players.Humans.Clear();
        for (var i = 0; i < 10; i++) f.Tick();

        f.Join(alive: true);
        f.Tick(); f.Tick();
        Assert.True(f.Manager.IsPreparing);
        Assert.Null(f.Manager.CurrentRound);
        f.Tick();
        Assert.Same(f.Round, f.Manager.CurrentRound);
    }

    [Fact]
    public void StoppedPreparationDoesNotRestartWhenAnOldCallbackOrNewPlayerArrives()
    {
        using var f = new Fixture();
        f.Manager.ForceStop();
        f.Join(alive: true);
        for (var i = 0; i < 10; i++) f.Tick();

        Assert.True(f.Timer.IsCancellationRequested);
        Assert.False(f.Manager.IsPreparing);
        Assert.Null(f.Manager.CurrentRound);
        Assert.Equal(0, f.Factory.Calls);
    }

    private sealed class Fixture : IDisposable
    {
        private const int Delay = 3;
        private bool _alive;
        public Mock<IPlayer> Player { get; } = new();
        public PlayerRegistry Players { get; } = new();
        public CancellationTokenSource Timer { get; } = new();
        public TestRound Round { get; }
        public RoundFactory Factory { get; }
        public RoundManager Manager { get; }

        public Fixture()
        {
            var core = new Mock<ISwiftlyCore> { DefaultValue = DefaultValue.Mock };
            core.Setup(value => value.PlayerManager.GetAllPlayers()).Returns(() => Players.GetAllPlayers().ToArray());
            Player.SetupGet(value => value.IsValid).Returns(true);
            Player.SetupGet(value => value.IsAlive).Returns(() => _alive);
            Players.OnRespawn = () => _alive = true;
            Round = new TestRound(core.Object, Players);
            Factory = new RoundFactory(Round);
            Manager = new RoundManager(core.Object, Options.Create(new ZombiePlagueCoreConfig { PreStartDelay = Delay }),
                Players, new DamageMovementRestore(core.Object, Players), new RoundRegistry(), Factory,
                Mock.Of<IHookPublisher>(), () => throw new InvalidOperationException("Переводы здесь не запрашиваются."));

            // Воспроизводим состояние после Prepare без нативного воспроизведения звука CS2.
            Field("_preparationTimer").SetValue(Manager, Timer);
            Field("_remainingPreparationTime").SetValue(Manager, Delay);
        }

        public void Join(bool alive)
        {
            _alive = alive;
            Players.Humans.Add(Player.Object);
        }

        public void Tick() => typeof(RoundManager).GetMethod("OnPrepareTask", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Manager, null);
        private static FieldInfo Field(string name) => typeof(RoundManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
        public void Dispose() { Manager.ForceStop(); Timer.Dispose(); }
    }

    private sealed class TestRound(ISwiftlyCore core, PlayerRegistry players) : RoundBase(core, players, () => null!)
    {
        public override string Id => RoundIds.Infection;
        public override string Name => "Test round";
        public int Starts { get; private set; }
        protected override bool OnStart() { Starts++; return players.GetAllAliveHumans().Any(); }
        protected override void OnEnd() { }
    }

    private sealed class RoundFactory(TestRound round) : IRoundFactory
    {
        public int Calls { get; private set; }
        public RoundBase Create<TRound>() where TRound : RoundBase { Calls++; return round; }
        public RoundBase Create(IRoundConfig config) => throw new NotSupportedException();
        public bool TryCreate(string id, [NotNullWhen(true)] out RoundBase? value) { value = null; return false; }
    }

    private sealed class RoundRegistry : IRoundRegistrator
    {
        public IEnumerable<IRoundConfig> GetAll() => [];
        public IEnumerable<IRoundConfig> GetAllEnabled() => [];
        public void Register() { }
    }

    private sealed class PlayerRegistry : PlayerRegistryContract
    {
        public List<IPlayer> Humans { get; } = [];
        public Action OnRespawn { get; set; } = null!;
        public int Respawns { get; private set; }
        public IEnumerable<IPlayer> GetAllPlayers() => Humans;
        public IEnumerable<IPlayer> GetAllHumans() => Humans;
        public IEnumerable<IPlayer> GetAllAliveHumans() => Humans.Where(player => player.IsAlive);
        public IEnumerable<IPlayer> GetAllZombies() => [];
        public IEnumerable<IPlayer> GetAllAliveZombies() => [];
        public bool TrySetHuman(IPlayer player) => IsHuman(player);
        public bool TryRespawn(IPlayer player) { Respawns++; OnRespawn(); return true; }
        public bool Remove(IPlayer player) => Humans.Remove(player);
        public void Clear() => Humans.Clear();
        public bool TryInfect(IPlayer player, IPlayer? infector = null) => throw new NotSupportedException();
        public bool TryDisinfect(IPlayer player) => throw new NotSupportedException();
        public bool TrySetNemesis(IPlayer player, [NotNullWhen(true)] out IZombie? zombie) => throw new NotSupportedException();
        public bool TrySetSurvivor(IPlayer player, [NotNullWhen(true)] out IHuman? human) => throw new NotSupportedException();
        public bool TryApplyRole(IPlayer player) => throw new NotSupportedException();
        public bool TryDeactivateRole(IPlayer player) => throw new NotSupportedException();
        public bool IsHuman(IPlayer player) => Humans.Any(human => ReferenceEquals(human, player));
        public bool IsZombie(IPlayer player) => false;
        public bool IsNemesis(IPlayer player) => false;
        public bool IsSurvivor(IPlayer player) => false;
        public bool TryGetHuman(IPlayer player, [NotNullWhen(true)] out IHuman? human) => throw new NotSupportedException();
        public bool TryGetZombie(IPlayer player, [NotNullWhen(true)] out IZombie? zombie) => throw new NotSupportedException();
        public bool TryGetRole(IPlayer player, [NotNullWhen(true)] out IPlayerRole? role) => throw new NotSupportedException();
    }
}
