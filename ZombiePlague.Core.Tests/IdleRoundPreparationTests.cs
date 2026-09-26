using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Common.Hooks.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Services;
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
    public void EmptyServerKeepsPreparationWithoutStartingARound()
    {
        using var f = new Fixture();
        for (var i = 0; i < 100; i++) f.Tick();

        Assert.True(f.Manager.IsPreparing);
        Assert.False(f.Timer.IsCancellationRequested);
        Assert.Null(f.Manager.CurrentRound);
        Assert.Equal(0, f.Factory.Calls);
    }

    [Fact]
    public void SinglePlayerIsNeverInfectedAndWaitsForTheSecondPlayer()
    {
        using var f = new Fixture();
        f.Join();
        for (var i = 0; i < 100; i++) f.Tick();

        Assert.True(f.Manager.IsPreparing);
        Assert.Null(f.Manager.CurrentRound);
        Assert.Equal(0, f.Round.Starts);

        f.Join();
        f.Tick(); f.Tick();
        Assert.Null(f.Manager.CurrentRound);
        f.Tick();

        Assert.Same(f.Round, f.Manager.CurrentRound);
        Assert.False(f.Manager.IsPreparing);
        Assert.Equal(1, f.Round.Starts);
    }

    [Theory]
    [InlineData(Team.Spectator)]
    [InlineData(Team.None)]
    public void SpectatorsAndPlayersOutsideTeamsAreNotCounted(Team team)
    {
        using var f = new Fixture();
        f.Join();
        f.Join(team: team);
        for (var i = 0; i < 10; i++) f.Tick();

        Assert.True(f.Manager.IsPreparing);
        Assert.Null(f.Manager.CurrentRound);
    }

    [Fact]
    public void ConnectingPlayersAreNotCounted()
    {
        using var f = new Fixture();
        f.Join();
        var connecting = f.Join();
        connecting.Valid = false;
        for (var i = 0; i < 10; i++) f.Tick();

        Assert.True(f.Manager.IsPreparing);
        Assert.Null(f.Manager.CurrentRound);
    }

    [Fact]
    public void PlayerLeavingDuringTheCountdownRestoresTheFullCountdown()
    {
        using var f = new Fixture();
        f.Join();
        var second = f.Join();
        f.Tick(); f.Tick();
        f.Leave(second);
        for (var i = 0; i < 10; i++) f.Tick();

        f.Join();
        f.Tick(); f.Tick();
        Assert.True(f.Manager.IsPreparing);
        Assert.Null(f.Manager.CurrentRound);
        f.Tick();
        Assert.Same(f.Round, f.Manager.CurrentRound);
    }

    [Fact]
    public void DeathOfTheOnlyWaitingPlayerStartsTheNextRound()
    {
        using var f = new Fixture();
        var player = f.Join();
        f.Tick();

        player.Alive = false;
        f.Manager.OnPlayerDeath(f.Death(player));
        f.Manager.OnPlayerDeath(f.Death(player));
        for (var i = 0; i < 5; i++) f.Tick();

        f.Game.Verify(game => game.TerminateRound(RoundEndReason.RoundDraw, It.IsAny<float>()), Times.Once);
        Assert.Empty(f.Delayed);
        Assert.Equal(0, f.Players.Respawns);
        Assert.False(player.Alive);
    }

    [Fact]
    public void DeathDuringTheCountdownRespawnsThePlayerAndDelaysTheStart()
    {
        using var f = new Fixture();
        f.Join();
        var second = f.Join();

        second.Alive = false;
        f.Manager.OnPlayerDeath(f.Death(second));
        for (var i = 0; i < 5; i++) f.Tick();

        f.Game.Verify(game => game.TerminateRound(It.IsAny<RoundEndReason>(), It.IsAny<float>()), Times.Never);
        Assert.Null(f.Manager.CurrentRound);
        Assert.Single(f.Delayed);

        f.RunDelayed();
        Assert.True(second.Alive);
        f.Tick();
        Assert.Same(f.Round, f.Manager.CurrentRound);
    }

    [Fact]
    public void PreparationRespawnsTeamPlayersThatNeverSpawned()
    {
        using var f = new Fixture();
        var first = f.Join(alive: false);
        var second = f.Join(alive: false, role: false);
        f.Tick();

        Assert.True(first.Alive);
        Assert.True(second.Alive);
        Assert.Contains(second.Object, f.Players.Humans);
    }

    [Fact]
    public void JoiningATeamDuringPreparationRespawnsThePlayerImmediately()
    {
        using var f = new Fixture();
        var player = f.Join(alive: false, role: false, team: Team.Spectator);
        player.Team = Team.CT;

        f.Manager.OnPlayerTeam(f.TeamChange(player, Team.Spectator, Team.CT));

        Assert.True(player.Alive);
        Assert.Contains(player.Object, f.Players.Humans);
    }

    [Fact]
    public void RoundThatCannotStartKeepsThePreparationRunning()
    {
        using var f = new Fixture();
        f.Round.CanStartValue = false;
        f.Join(); f.Join();
        for (var i = 0; i < 10; i++) f.Tick();

        Assert.True(f.Manager.IsPreparing);
        Assert.Null(f.Manager.CurrentRound);
        Assert.Equal(0, f.Round.Starts);

        f.Round.CanStartValue = true;
        f.Tick();
        Assert.Same(f.Round, f.Manager.CurrentRound);
    }

    [Fact]
    public void FailedStartReturnsTheServerToPreparation()
    {
        using var f = new Fixture();
        f.Round.StartResult = false;
        f.Join(); f.Join();
        for (var i = 0; i < 3; i++) f.Tick();

        Assert.Equal(1, f.Round.Starts);
        Assert.Null(f.Manager.CurrentRound);
        Assert.True(f.Manager.IsPreparing);
    }

    [Fact]
    public void StoppedPreparationDoesNotRestartWhenAnOldCallbackOrNewPlayerArrives()
    {
        using var f = new Fixture();
        f.Manager.ForceStop();
        f.Join(); f.Join();
        for (var i = 0; i < 10; i++) f.Tick();

        Assert.True(f.Timer.IsCancellationRequested);
        Assert.False(f.Manager.IsPreparing);
        Assert.Null(f.Manager.CurrentRound);
        Assert.Equal(0, f.Factory.Calls);
    }

    private sealed class TestPlayer
    {
        public Mock<IPlayer> Mock { get; } = new();
        public IPlayer Object => Mock.Object;
        public bool Alive { get; set; }
        public bool Valid { get; set; } = true;
        public Team Team { get; set; }

        public TestPlayer(int id)
        {
            var controller = new Mock<CCSPlayerController>();
            controller.SetupGet(value => value.Team).Returns(() => Team);
            Mock.SetupGet(value => value.PlayerID).Returns(id);
            Mock.SetupGet(value => value.SessionId).Returns((ulong)id);
            Mock.SetupGet(value => value.IsValid).Returns(() => Valid);
            Mock.SetupGet(value => value.IsAlive).Returns(() => Valid && Alive);
            Mock.SetupGet(value => value.Controller).Returns(controller.Object);
        }
    }

    private sealed class Fixture : IDisposable
    {
        private const int Delay = 3;
        private readonly List<TestPlayer> _connected = [];
        public PlayerRegistry Players { get; } = new();
        public CancellationTokenSource Timer { get; } = new();
        public Mock<IGameService> Game { get; } = new();
        public List<Action> Delayed { get; } = [];
        public TestRound Round { get; }
        public RoundFactory Factory { get; }
        public RoundManager Manager { get; }

        public Fixture()
        {
            var core = new Mock<ISwiftlyCore> { DefaultValue = DefaultValue.Mock };
            core.Setup(value => value.PlayerManager.GetAllPlayers())
                .Returns(() => _connected.Select(player => player.Object).ToArray());
            core.Setup(value => value.PlayerManager.GetPlayer(It.IsAny<int>()))
                .Returns((int id) => _connected.FirstOrDefault(player => player.Object.PlayerID == id)?.Object);
            core.Setup(value => value.Scheduler.DelayAndRepeatBySeconds(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<Action>()))
                .Returns(() => new CancellationTokenSource());
            core.Setup(value => value.Scheduler.DelayBySeconds(It.IsAny<float>(), It.IsAny<Action>()))
                .Returns((float _, Action task) => { Delayed.Add(task); return new CancellationTokenSource(); });
            core.Setup(value => value.Scheduler.NextWorldUpdate(It.IsAny<Action>()))
                .Callback((Action task) => task());
            core.SetupGet(value => value.Game).Returns(Game.Object);
            Players.OnRespawn = player => Find(player).Alive = true;
            Players.OnHumanize = player => Find(player).Team = Team.CT;
            Round = new TestRound(core.Object, Players);
            Factory = new RoundFactory(Round);
            Manager = new RoundManager(core.Object, Options.Create(new ZombiePlagueCoreConfig { PreStartDelay = Delay }),
                Players, new DamageMovementRestore(core.Object, Players), new RoundRegistry(), Factory,
                Mock.Of<IHookPublisher>(), () => throw new InvalidOperationException("Переводы здесь не запрашиваются."));

            // Воспроизводим состояние после Prepare без нативного воспроизведения звука CS2.
            Field("_preparationTimer").SetValue(Manager, Timer);
            Field("_remainingPreparationTime").SetValue(Manager, Delay);
        }

        public TestPlayer Join(bool alive = true, bool role = true, Team team = Team.CT)
        {
            var player = new TestPlayer(_connected.Count + 1) { Alive = alive, Team = team };
            _connected.Add(player);
            if (role && team is Team.T or Team.CT) Players.Humans.Add(player.Object);
            return player;
        }

        public void Leave(TestPlayer player)
        {
            _connected.Remove(player);
            Players.Humans.Remove(player.Object);
        }

        public EventPlayerDeath Death(TestPlayer player)
        {
            var death = new Mock<EventPlayerDeath>();
            death.As<IDisposable>();
            death.SetupGet(value => value.UserIdPlayer).Returns(player.Object);
            return death.Object;
        }

        public EventPlayerTeam TeamChange(TestPlayer player, Team oldTeam, Team team)
        {
            var change = new Mock<EventPlayerTeam>();
            change.As<IDisposable>();
            change.SetupGet(value => value.UserIdPlayer).Returns(player.Object);
            change.SetupGet(value => value.OldTeam).Returns((byte)oldTeam);
            change.SetupGet(value => value.Team).Returns((byte)team);
            return change.Object;
        }

        public void RunDelayed()
        {
            var tasks = Delayed.ToArray();
            Delayed.Clear();
            foreach (var task in tasks) task();
        }

        private TestPlayer Find(IPlayer player) => _connected.Single(value => ReferenceEquals(value.Object, player));

        public void Tick() => typeof(RoundManager).GetMethod("OnPrepareTask", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(Manager, null);
        private static FieldInfo Field(string name) => typeof(RoundManager).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!;
        public void Dispose() { Manager.ForceStop(); Timer.Dispose(); }
    }

    private sealed class TestRound(ISwiftlyCore core, PlayerRegistry players) : RoundBase(core, players, () => null!)
    {
        public override string Id => RoundIds.Infection;
        public override string Name => "Test round";
        public int Starts { get; private set; }
        public bool CanStartValue { get; set; } = true;
        public bool StartResult { get; set; } = true;
        public override bool CanStart() => CanStartValue;
        protected override bool OnStart() { Starts++; return StartResult && players.GetAllAliveHumans().Count() > 1; }
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
        public Action<IPlayer> OnRespawn { get; set; } = null!;
        public Action<IPlayer> OnHumanize { get; set; } = null!;
        public int Respawns { get; private set; }
        public IEnumerable<IPlayer> GetAllPlayers() => Humans;
        public IEnumerable<IPlayer> GetAllHumans() => Humans;
        public IEnumerable<IPlayer> GetAllAliveHumans() => Humans.Where(player => player.IsAlive);
        public IEnumerable<IPlayer> GetAllZombies() => [];
        public IEnumerable<IPlayer> GetAllAliveZombies() => [];

        public bool TrySetHuman(IPlayer player)
        {
            if (!IsHuman(player)) Humans.Add(player);
            OnHumanize(player);
            return true;
        }

        public bool TryRespawn(IPlayer player) { Respawns++; OnRespawn(player); return true; }
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

        public bool TryGetRole(IPlayer player, [NotNullWhen(true)] out IPlayerRole? role)
        {
            role = IsHuman(player) ? new Role(player) : null;
            return role is not null;
        }
    }

    private sealed class Role(IPlayer owner) : IPlayerRole
    {
        public IPlayer Owner => owner;
        public void Bind() { }
        public void Unbind() { }
    }
}
