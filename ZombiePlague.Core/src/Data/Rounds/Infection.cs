using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data.Rounds;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Abilities;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds;

internal sealed class Infection(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    InfectionConfig config
) : InfectionBase(core, playerManager)
{
    private readonly Dictionary<int, CancellationTokenSource> _respawnTimers = [];
    
    public override string Id => RoundIds.Infection;
    
    public override string Name => config.Name;
    
    protected override bool OnStart()
    {
        var humans = PlayerManager.GetAllAliveHumans().ToArray();

        var zombies = PlayerManager.GetAllAliveZombies().ToArray();

        if (zombies.Length > 0)
        {
            var zombie = zombies[Random.Shared.Next(zombies.Length)];

            return SetFirstZombie(zombie);
        }

        if (humans.Length == 0)
        {
            return false;
        }

        var candidate =
            humans[Random.Shared.Next(humans.Length)];

        return SetFirstZombie(candidate);
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
        
        return humansCount > 1;
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

    protected override HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var playerId = @event.PlayerID;
        
        CancelRespawnTimer(playerId);

        return HookResult.Continue;
    }
    
    public override bool TryRespawnPlayer(IPlayer player)
    {
        if (!player.IsValid || player.IsAlive)
        {
            return false;
        }

        if (!PlayerManager.IsZombie(player) && !PlayerManager.TryInfect(player))
        {
            return false;
        }

        return PlayerManager.TryRespawn(player);
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
    
    private bool SetFirstZombie(IPlayer player)
    {
        if (!PlayerManager.IsZombie(player) && !PlayerManager.TryInfect(player))
        {
            return false;
        }

        if (!PlayerManager.TryGetZombie(player, out var firstZombie))
        {
            return false;
        }

        var health = (int)Math.Round(
            firstZombie.ZClass.Health *
            config.FirstZombieHealthRatio
        );

        player.SetHealth(health);

        if (!config.FirstZombieLeap)
        {
            var leap = firstZombie.ZClass.Abilities
                .OfType<Leap>()
                .FirstOrDefault();

            leap?.UnHook();
        }

        SoundExt.PlayAt(player, config.MusicSoundName, 1.5f);

        Core.PlayerManager.SendCenter($"Первый заражённый => {player.Name}");

        return true;
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