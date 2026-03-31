using Common.Di;
using CustomKnife.Data.Models;
using CustomKnife.Data.Services.Contracts;
using CustomKnife.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZPApi;
using ZPApi.Data;

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
    private readonly Lazy<IKnifeMenuService> _knifeMenuService = DependencyResolver.GetRequiredServiceLazy<IKnifeMenuService>();
    
    private Guid _guidOnPlayerEquipEventPost = Guid.Empty;
    private Guid _guidOnPlayerSpawnEventPost = Guid.Empty;
    private Guid _guidOnPlayerHurtEventPost = Guid.Empty;
    private Guid _guidOnRoundStartEventPost = Guid.Empty;

    public static IZServiceApi ZServiceApi = null!;
    
    public static readonly Dictionary<IPlayer, IKnife> PlayerKnifes = [];
    public static readonly List<IKnife> RegisteredKnifes = [];
    
    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
        ZServiceApi = interfaceManager.GetSharedInterface<IZServiceApi>(IZServiceApi.SharedApiKey);
    }
    
    protected override void OnReady()
    {
        foreach (var knife in _knifeService.Value.GetRegisteredKnives())
        {
            RegisteredKnifes.Add(knife);
        }
        
        _guidOnPlayerEquipEventPost = core.GameEvent.HookPost<EventItemEquip>(PlayerEquipEvent);
        _guidOnPlayerSpawnEventPost = core.GameEvent.HookPost<EventPlayerSpawn>(PlayerSpawnEvent);
        _guidOnPlayerHurtEventPost = core.GameEvent.HookPost<EventPlayerHurt>(PlayerHurtEvent);
        _guidOnRoundStartEventPost = core.GameEvent.HookPost<EventRoundStart>(EventRoundStart);
        core.Event.OnEntityTakeDamage += OnEntityTakeDamage;
        ZServiceApi.EventSubscriber.OnGameRoundStarted += OnGameRoundStarted;
        
        core.Command.RegisterCommand(
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
        core.Event.OnEntityTakeDamage -= OnEntityTakeDamage;
        ZServiceApi.EventSubscriber.OnGameRoundStarted -= OnGameRoundStarted;
    }

    private void OnGameRoundStarted(IRound round)
    {
        if (ZServiceApi.IsSurvivorRound(round) || ZServiceApi.IsArmageddonRound(round))
        {
            var alivePlayers = core.PlayerManager.GetAlive();

            foreach (var player in alivePlayers)
            {
                if (ZServiceApi.IsSurvivor(player))
                {
                    _knifeService.Value.TryGiveKnife(player);
                }
            }
        }
    }
    
    private void OnEntityTakeDamage(IOnEntityTakeDamageEvent @event)
    {
        _knifeService.Value.TryApplyKnifeDamage(@event);
    }
    
    private HookResult PlayerSpawnEvent(EventPlayerSpawn @event)
    {
        _knifeService.Value.TryGiveKnife(@event.UserIdPlayer);
        
        return HookResult.Continue;
    }
    
    private HookResult EventRoundStart(EventRoundStart @event)
    {
        var alivePlayers = core.PlayerManager.GetAlive();

        foreach (var player in alivePlayers)
        {
            core.Scheduler.NextWorldUpdate(()=> _knifeService.Value.TryGiveKnife(player));
        }
        
        return HookResult.Continue;
    }
    
    private HookResult PlayerHurtEvent(EventPlayerHurt @event)
    {
        _knifeService.Value.TryApplyProperties(@event.UserIdPlayer);
        
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