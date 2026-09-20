using Common.Database.Storages;
using Common.Database.Tasks;
using CustomKnife.Data.Knives;
using CustomKnife.Data.Models;
using CustomKnife.Data.Registrator;
using CustomKnife.Data.Services;
using CustomKnife.Data.Services.Contracts;
using CustomKnife.Data.Store;
using CustomKnife.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;
using ZombiePlague.Api;

namespace CustomKnife.Core.Tests;

public sealed class ZombieKnifePreferenceTests
{
    [Fact]
    public async Task SelectionWhileZombieSurvivesLateLoadAndDisconnectWithoutApplyingHumanProperties()
    {
        const ulong steamId = 123;
        var load = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var saved = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        var persistence = new Mock<IPlayerKnifePersistenceService>();
        persistence.Setup(value => value.LoadAsync(steamId, It.IsAny<CancellationToken>())).Returns(load.Task);
        persistence.Setup(value => value.SaveAsync(steamId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<ulong, string, CancellationToken>((_, id, _) => saved.TrySetResult(id)).Returns(Task.CompletedTask);
        using var tasks = new DatabaseTaskTracker(NullLogger<DatabaseTaskTracker>.Instance);
        var queue = new SteamIdOperationQueue();
        var preferences = new PlayerKnifeService(new PlayerSessionStore<PlayerKnifePreferences>(), persistence.Object, tasks, queue);
        preferences.Initialize(steamId);
        IKnife selected = KnifeDefaults.Fallback with { InternalName = "knife_selected", Speed = 350, Gravity = 500 };
        var registry = new Mock<IKnivesRegistry>();
        registry.Setup(value => value.TryGet(selected.InternalName, out selected)).Returns(true);
        var authorization = new Mock<IKnifeAuthorizationService>();
        authorization.Setup(value => value.CanUse(It.IsAny<IPlayer>(), selected)).Returns(true);
        var core = new Mock<ISwiftlyCore>(MockBehavior.Strict);
        var pawn = Mock.Of<CCSPlayerPawn>(value => value.IsValid);
        var controller = Mock.Of<CCSPlayerController>(value => value.Team == Team.CT);
        var player = Mock.Of<IPlayer>(value => value.IsValid && value.IsAlive && value.SteamID == steamId
            && value.PlayerPawn == pawn && value.Controller == controller);
        var zombies = new Mock<IZombiePlagueApi>(MockBehavior.Strict);
        zombies.Setup(value => value.IsInfected(player)).Returns(true);
        using var service = new KnifeService(core.Object, registry.Object, preferences, zombies.Object, authorization.Object);

        service.SelectKnife(player, selected);
        Assert.Same(selected, service.GetKnife(player));
        Assert.False(service.TryGiveKnife(player));
        Assert.False(service.TryApplyProperties(player));
        var hurt = new Mock<EventPlayerHurt>();
        hurt.As<IDisposable>();
        hurt.SetupGet(value => value.AttackerPlayer).Returns(player);
        Assert.False(service.TryApplyKnifeKnockback(hurt.Object));
        // Strict mocks обнаружат обращение к выдаче оружия, движению или отбрасыванию зомби.
        core.VerifyNoOtherCalls();

        load.SetResult(KnifeDefaults.DefaultKnifeId);
        await queue.RunAsync(steamId, () => Task.CompletedTask);
        Assert.Equal(selected.InternalName, preferences.GetKnifeId(steamId));
        preferences.Remove(steamId);
        Assert.Equal(selected.InternalName, await saved.Task.WaitAsync(TimeSpan.FromSeconds(3)));
        Assert.Null(preferences.GetKnifeId(steamId));
    }
}
