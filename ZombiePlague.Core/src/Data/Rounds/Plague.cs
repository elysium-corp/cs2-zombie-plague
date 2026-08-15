using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Events;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds;

internal sealed class Plague(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    IEventPublisher eventPublisher,
    PlagueConfig config
) : InfectionBase(core, playerManager, eventPublisher)
{
    private readonly Dictionary<int, CancellationTokenSource> _respawnTimers = [];
    
    public override string Name => config.Name;
    
    protected override void OnStart()
    {
        var humans = PlayerManager
            .GetAllAliveHumans()
            .ToArray();

        var infectionRatio = config.ZombieSpawnRatio;

        if (infectionRatio is <= 0.0f or >= 1.0f)
        {
            throw new InvalidOperationException($"{nameof(config.ZombieSpawnRatio)} must be between 0 and 1.");
        }

        var targetInfectedCount = Math.Clamp(
            (int)Math.Ceiling(humans.Length * infectionRatio),
            min: 1,
            max: humans.Length - 1
        );

        Random.Shared.Shuffle(humans);

        var successfulInfections = 0;

        foreach (var human in humans)
        {
            if (!PlayerManager.TryInfect(human))
            {
                continue;
            }

            successfulInfections++;

            if (successfulInfections >= targetInfectedCount)
            {
                break;
            }
        }
        
        if (config.IsMusicEnabled && !string.IsNullOrWhiteSpace(config.MusicSoundName))
        {
            SoundExt.PlayGlobal(config.MusicSoundName);
        }
        
        Core.PlayerManager.SendCenter($"Массовое заражение!");
    }
    
    protected override void OnEnd()
    {
        var timers = _respawnTimers.Values.ToArray();
        _respawnTimers.Clear();

        foreach (var timer in timers)
        {
            timer.Cancel();
        }
    }
    
    public override bool CanStart()
    {
        var humansCount = PlayerManager.GetAllAliveHumans().Count();
        
        return humansCount >= config.MinimumHumansRequired;
    }
    
    protected override HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        var player = @event.UserIdPlayer;

        if (player is not { IsValid: true }) return HookResult.Continue;
        
        if (PlayerManager.IsZombie(player) ||
            PlayerManager.IsHuman(player) &&
            PlayerManager.GetAllAliveHumans().Any() &&
            PlayerManager.TryInfect(player)
           )
        {
            ScheduleZombieRespawn(player);
        }

        return HookResult.Continue;
    }

    protected override HookResult OnPlayerConnectedFull(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;

        if (player is not { IsValid: true }) return HookResult.Continue;

        PlayerManager.TryInfect(player);

        ScheduleZombieRespawn(player);
        
        return HookResult.Continue; 
    }

    protected override HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var playerId = @event.PlayerID;
        
        CancelRespawnTimer(playerId);

        return HookResult.Continue;
    }
    
    private void ScheduleZombieRespawn(IPlayer player)
    {
        if (!config.ZombieRevived || !player.IsValid || player.IsAlive || !PlayerManager.IsZombie(player)) return;

        CancelRespawnTimer(player.PlayerID);

        if (config.ZombieSpawnTime <= 0)
        {
            Core.Scheduler.NextWorldUpdate(() => Respawn(player));

            return;
        }

        var playerId = player.PlayerID;

        _respawnTimers[playerId] = Core.Scheduler.DelayBySeconds(config.ZombieSpawnTime, () =>
            {
                _respawnTimers.Remove(playerId);
                Respawn(player);
            }
        );
    }
    
    private void Respawn(IPlayer player)
    {
        if (!player.IsValid || player.IsAlive || !PlayerManager.IsZombie(player))
        {
            return;
        }

        PlayerManager.TryRespawn(player);
    }

    private void CancelRespawnTimer(int playerId)
    {
        if (_respawnTimers.Remove(playerId, out var timer))
        {
            timer.Cancel();
        }
    }
}