using Common.Di;
using CustomKnife.Data.Models;
using CustomKnife.Data.Services.Contracts;
using CustomKnife.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;
using ZombiePlague.Api.Data;

namespace CustomKnife;

[PluginMetadata(
    Id = "CustomKnife.Core",
    Version = "0.1.0",
    Name = "[ZP] CustomKnife",
    Author = "illusion & fdrinv",
    Description = "Adds a system of custom knives"
)]
internal sealed partial class CustomKnife(ISwiftlyCore core) : Plugin<CustomKnifeModule>(core)
{
    private readonly Lazy<IKnifeService> _knifeService = DependencyResolver.GetRequiredServiceLazy<IKnifeService>();

    private readonly Lazy<IKnifeMenuService> _knifeMenuService =
        DependencyResolver.GetRequiredServiceLazy<IKnifeMenuService>();

    private Guid _guidOnPlayerEquipEventPost = Guid.Empty;
    private Guid _guidOnPlayerSpawnEventPost = Guid.Empty;
    private Guid _guidOnPlayerHurtEventPost = Guid.Empty;
    private Guid _guidOnRoundStartEventPost = Guid.Empty;

    public static IZombiePlagueApi ZombiePlagueApi = null!;

    public static readonly Dictionary<IPlayer, IKnife> PlayerKnifes = [];
    public static readonly List<IKnife> RegisteredKnifes = [];

    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
        ZombiePlagueApi = interfaceManager.GetSharedInterface<IZombiePlagueApi>(IZombiePlagueApi.SharedApiKey);
    }

    protected override void OnReady()
    {
        foreach (var knife in _knifeService.Value.GetRegisteredKnives())
        {
            RegisteredKnifes.Add(knife);
        }

        _guidOnPlayerEquipEventPost = Core.GameEvent.HookPost<EventItemEquip>(PlayerEquipEvent);
        _guidOnPlayerSpawnEventPost = Core.GameEvent.HookPost<EventPlayerSpawn>(PlayerSpawnEvent);
        _guidOnPlayerHurtEventPost = Core.GameEvent.HookPost<EventPlayerHurt>(PlayerHurtEvent);
        _guidOnRoundStartEventPost = Core.GameEvent.HookPost<EventRoundStart>(EventRoundStart);
        Core.GameHooks.Entities.TakeDamage.Pre += OnEntityTakeDamage;
        ZombiePlagueApi.EventSubscriber.OnRoundStarted += OnRoundStarted;

        Core.Command.RegisterCommand(
            commandName: "knife",
            handler: KnifeMenuHandler,
            registerRaw: true
        );
    }

    protected override void OnUnload()
    {
        Core.GameEvent.Unhook(_guidOnPlayerEquipEventPost);
        Core.GameEvent.Unhook(_guidOnPlayerSpawnEventPost);
        Core.GameEvent.Unhook(_guidOnPlayerHurtEventPost);
        Core.GameEvent.Unhook(_guidOnRoundStartEventPost);
        Core.GameHooks.Entities.TakeDamage.Pre -= OnEntityTakeDamage;
        ZombiePlagueApi.EventSubscriber.OnRoundStarted -= OnRoundStarted;
    }

    private void OnRoundStarted(IRound round)
    {
        if (ZombiePlagueApi.IsSurvivorRound(round) || ZombiePlagueApi.IsArmageddonRound(round))
        {
            var alivePlayers = Core.PlayerManager.GetAlive();

            foreach (var player in alivePlayers)
            {
                if (ZombiePlagueApi.IsSurvivor(player))
                {
                    _knifeService.Value.TryGiveKnife(player);
                }
            }
        }
    }

    private void OnEntityTakeDamage(ref TakeDamageEntityPreContext @event)
    {
        _knifeService.Value.TryApplyKnifeDamage(ref @event);
    }

    private HookResult PlayerSpawnEvent(EventPlayerSpawn @event)
    {
        var player = @event.UserIdPlayer;
        if (player == null)
        {
            return HookResult.Continue;
        }

        _knifeService.Value.TryGiveKnife(player);

        return HookResult.Continue;
    }

    private HookResult EventRoundStart(EventRoundStart @event)
    {
        var alivePlayers = Core.PlayerManager.GetAlive();

        foreach (var player in alivePlayers)
        {
            Core.Scheduler.NextWorldUpdate(() => _knifeService.Value.TryGiveKnife(player));
        }

        return HookResult.Continue;
    }

    private HookResult PlayerHurtEvent(EventPlayerHurt @event)
    {
        var player = @event.UserIdPlayer;
        
        Core.Scheduler.NextTick(() => _knifeService.Value.TryApplyProperties(player));

        _knifeService.Value.TryApplyKnifeKnockback(@event);

        return HookResult.Continue;
    }

    private HookResult PlayerEquipEvent(EventItemEquip @event)
    {
        _knifeService.Value.TryApplyProperties(@event.UserIdPlayer);

        return HookResult.Continue;
    }

    private void KnifeMenuHandler(ICommandContext context)
    {
        var player = context.Sender;

        if (player == null)
        {
            return;
        }

        _knifeMenuService.Value.Show(player);
    }
}