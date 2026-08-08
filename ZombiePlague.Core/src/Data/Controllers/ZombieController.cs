using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data.Store;
using ZombiePlague.Core.Data.Entities.Zombie;
using ZombiePlague.Core.Data.Entities.Zombie.Classes;
using ZombiePlague.Core.Data.Entities.Zombie.Factory;
using ZombiePlague.Core.Store.Contracts;

namespace ZombiePlague.Core.Data.Controllers;

internal sealed class ZombieController(
    ISwiftlyCore core,
    IPlayerRepository playerRepository,
    IZClassFactory zClassFactory)
{
    public IZombie? Create(IPlayer player)
    {
        if (!player.IsValid) return null;

        var classId = playerRepository.GetZClassId(player);
        var zClass = zClassFactory.CreateOrDefault(classId);

        return Zombie.Create(core, player, zClass);
    }

    public IZombie? CreateNemesis(IPlayer player)
    {
        if (!player.IsValid) return null;

        var zClass = zClassFactory.Create<ZNemesis>();

        return Zombie.Create(core, player, zClass);
    }
}