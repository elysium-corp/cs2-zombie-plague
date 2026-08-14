using System.Diagnostics.CodeAnalysis;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Entities;
using ZombiePlague.Core.Data.Entities.Human;
using ZombiePlague.Core.Data.Entities.Zombie;

namespace ZombiePlague.Core.Data.Managers.Contracts;

internal interface IPlayerManager
{
    IEnumerable<IPlayer> GetAllPlayers();
    
    IEnumerable<IPlayer> GetAllZombies();

    public IEnumerable<IPlayer> GetAllHumans();

    public IEnumerable<IPlayer> GetAllAliveHumans();

    public IEnumerable<IPlayer> GetAllAliveZombies();
    
    bool TrySetHuman(IPlayer player);

    bool TryInfect(
        IPlayer player,
        IPlayer? infector = null
    );

    bool TryDisinfect(IPlayer player);

    bool TrySetNemesis(IPlayer player, [NotNullWhen(true)] out IZombie? nemesis);

    bool TrySetSurvivor(IPlayer player, [NotNullWhen(true)] out IHuman? survivor);

    bool TryRespawn(IPlayer player);
    
    bool TryApplyRole(IPlayer player);

    bool TryDeactivateRole(IPlayer player);

    bool IsHuman(IPlayer player);

    bool IsZombie(IPlayer player);

    bool IsNemesis(IPlayer player);

    bool IsSurvivor(IPlayer player);

    bool TryGetHuman(
        IPlayer player,
        [NotNullWhen(true)] out IHuman? human
    );

    bool TryGetZombie(
        IPlayer player,
        [NotNullWhen(true)] out IZombie? zombie
    );
    
    bool TryGetRole(
        IPlayer player,
        [NotNullWhen(true)] out IPlayerRole? role
    );

    bool Remove(IPlayer player);

    void Clear();
}
