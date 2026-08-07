using System.Diagnostics.CodeAnalysis;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Events;
using ZombiePlague.Core.Data.Controllers;
using ZombiePlague.Core.Data.Entities;
using ZombiePlague.Core.Data.Entities.Human;
using ZombiePlague.Core.Data.Entities.Human.Classes;
using ZombiePlague.Core.Data.Entities.Zombie;
using ZombiePlague.Core.Data.Entities.Zombie.Classes;
using ZombiePlague.Core.Data.Managers.Contracts;

namespace ZombiePlague.Core.Data.Managers;

internal sealed class PlayerManager(
    HumanController humanController,
    ZombieController zombieController,
    IEventPublisher eventPublisher
) : IPlayerManager
{
    private readonly Dictionary<IPlayer, IPlayerRole> _players = [];

    public IEnumerable<IPlayer> GetAllPlayers()
    {
        return _players.Values.Select(player => player.Owner);
    }
    
    public IEnumerable<IPlayer> GetAllHumans()
    {
        return _players.Values
            .OfType<IHuman>()
            .Select(static human => human.Owner);
    }

    public IEnumerable<IPlayer> GetAllZombies()
    {
        return _players.Values
            .OfType<IZombie>()
            .Select(static zombie => zombie.Owner);
    }
    
    public IEnumerable<IPlayer> GetAllAliveHumans()
    {
        return _players.Values
            .OfType<IHuman>()
            .Select(static human => human.Owner)
            .Where(static player => player is { IsValid: true, IsAlive: true });
    }

    public IEnumerable<IPlayer> GetAllAliveZombies()
    {
        return _players.Values
            .OfType<IZombie>()
            .Select(static zombie => zombie.Owner)
            .Where(static player => player is { IsValid: true, IsAlive: true });
    }

    public bool TryInfect(IPlayer player, IPlayer? infector = null)
    {
        if (!player.IsValid || !IsHuman(player))
        {
            return false;
        }

        var zombie = zombieController.Create(player);

        if (zombie is null)
        {
            return false;
        }

        AddOrReplaceRole(zombie);

        eventPublisher.OnPlayerInfected(player, infector);

        return true;
    }

    public bool TryDisinfect(IPlayer player)
    {
        if (!player.IsValid || !IsZombie(player))
        {
            return false;
        }

        if (!humanController.TryCreate(player, out var human))
        {
            return false;
        }

        AddOrReplaceRole(human);

        eventPublisher.OnPlayerDisinfected(player);

        return true;
    }

    public bool TrySetHuman(IPlayer player)
    {
        if (!player.IsValid && !player.IsFakeClient)
        {
            return false;
        }

        if (!humanController.TryCreate(player, out var human))
        {
            return false;
        }

        AddOrReplaceRole(human);

        return true;
    }

    public bool TrySetNemesis(IPlayer player, [NotNullWhen(true)] out IZombie? nemesis)
    {
        nemesis = null;
        
        if (!player.IsValid || !player.IsAlive)
        {
            return false;
        }

        nemesis = zombieController.CreateNemesis(player);

        if (nemesis is null)
        {
            return false;
        }

        AddOrReplaceRole(nemesis);

        eventPublisher.OnPlayerInfected(player);

        return true;
    }
    
    public bool TrySetSurvivor(IPlayer player, [NotNullWhen(true)] out IHuman? survivor)
    {
        survivor = null;
        
        if (!player.IsValid || !player.IsAlive)
        {
            return false;
        }

        if (!humanController.TryCreateSurvivor(player, out var human))
        {
            return false;
        }

        AddOrReplaceRole(human);

        survivor = human;

        return true;
    }

    public bool TryRespawn(IPlayer player)
    {
        if (!player.IsValid || player.IsAlive || !_players.TryGetValue(player, out var role)) {
            return false;
        }

        role.Unbind();

        MoveToRoleTeam(role);

        player.Respawn();
        
        return true;
    }
    
    public bool TryApplyRole(IPlayer player)
    {
        if (!player.IsValid || !player.IsAlive || !_players.TryGetValue(player, out var role))
        {
            return false;
        }

        role.Unbind();

        MoveToRoleTeam(role);
        role.Bind();

        return true;
    }
    
    public bool TryDeactivateRole(IPlayer player)
    {
        if (!_players.TryGetValue(player, out var role))
        {
            return false;
        }

        role.Unbind();

        return true;
    }

    public bool IsZombie(IPlayer player)
    {
        return _players.GetValueOrDefault(player) is IZombie;
    }

    public bool IsHuman(IPlayer player)
    {
        return _players.GetValueOrDefault(player) is IHuman;
    }

    public bool IsNemesis(IPlayer player)
    {
        TryGetZombie(player, out var zombie);
        return zombie?.ZClass is ZNemesis;
    }

    public bool IsSurvivor(IPlayer player)
    {
        TryGetHuman(player, out var human);
        return human?.HClass is HSurvivor;
    }

    public bool TryGetZombie(IPlayer player, [NotNullWhen(true)] out IZombie? zombie)
    {
        zombie = _players.GetValueOrDefault(player) as IZombie;
        return zombie is not null;
    }

    public bool Remove(int playerId)
    {
        throw new NotImplementedException();
    }

    public bool TryGetHuman(IPlayer player, [NotNullWhen(true)] out IHuman? human)
    {
        human = _players.GetValueOrDefault(player) as IHuman;
        return human is not null;
    }

    public bool Remove(IPlayer player)
    {
        if (!_players.Remove(player, out var role))
        {
            return false;
        }

        role.Unbind();

        return true;
    }

    public void Clear()
    {
        foreach (var role in _players.Values)
        {
            role.Unbind();
        }

        _players.Clear();
    }

    private void AddOrReplaceRole(IPlayerRole nextRole)
    {
        var player = nextRole.Owner;

        if (_players.Remove(player, out var previousRole))
        {
            previousRole.Unbind();
        }

        _players[player] = nextRole;

        MoveToRoleTeam(nextRole);

        if (player.IsAlive)
        {
            nextRole.Bind();
        }
    }
    
    private static void MoveToRoleTeam(IPlayerRole role)
    {
        var team = role switch
        {
            IHuman => Team.CT,
            IZombie => Team.T,
            _ => throw new NotSupportedException($"Unsupported player role: {role.GetType().Name}")
        };

        role.Owner.SwitchTeam(team);
    }
}
