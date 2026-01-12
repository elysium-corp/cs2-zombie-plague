using CS2ZombiePlague.Data.Events;
using CS2ZombiePlague.Data.ZClasses;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Managers;

public class ZombieManager(IZombiePlayerFactory zombiePlayerFactory, ZClassMenu zClassMenu, IEventPublisher eventPublisher, ISwiftlyCore core)
{
    private readonly Dictionary<int, ZombiePlayer> _zombiePlayers = new();

    public ZombiePlayer? CreateZombie(IPlayer player, int? attackerId = null, int? victimId = null)
    {
        if (!player.IsValid)
        {
            return null;
        }

        if (attackerId != null && victimId != null)
        {
            FireFakeDeath(attackerId.Value, victimId.Value);
        }

        var zClass = GetZClassFromMenu(player.PlayerID);
        return _zombiePlayers[player.PlayerID] =
            zombiePlayerFactory.Create(player, this, zClass);
    }

    public ZombiePlayer? CreateNemesis(IPlayer player)
    {
        if (player is { IsValid: true })
        {
            var nemesis = DependencyManager.GetService<ZNemesis>();
            return _zombiePlayers[player.PlayerID] =
                zombiePlayerFactory.Create(player, this, nemesis, true);
        }

        return null;
    }

    public void Remove(IPlayer player)
    {
        _zombiePlayers[player.PlayerID].UnHookAbilities();
        _zombiePlayers.Remove(player.PlayerID);
    }

    public void RemoveAll()
    {
        foreach (var zPlayer in _zombiePlayers.Values)
        {
            zPlayer.UnHookAbilities();
        }

        _zombiePlayers.Clear();
    }

    public ZombiePlayer GetZombie(int playerId)
    {
        return _zombiePlayers[playerId];
    }

    public Dictionary<int, ZombiePlayer> GetAllZombies()
    {
        return _zombiePlayers;
    }

    public IZClass GetZClassFromMenu(int playerId)
    {
        return zClassMenu.GetPlayerZClass(playerId);
    }

    private void FireFakeDeath(int attackerId, int victimId)
    {
        core.GameEvent.FireAsync<EventPlayerDeath>((@event) =>
        {
            @event.UserId = victimId;
            @event.Attacker = attackerId;
            @event.Weapon = "knife";
            @event.Assister = -1;
        });
    }
}