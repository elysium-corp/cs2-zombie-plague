using System.Diagnostics.CodeAnalysis;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data.Store;
using ZombiePlague.Core.Data.Entities.Human;
using ZombiePlague.Core.Data.Entities.Human.Classes;
using ZombiePlague.Core.Data.Entities.Human.Factory;

namespace ZombiePlague.Core.Data.Controllers;

internal sealed class HumanController(ISwiftlyCore core, IPlayerRepository playerRepository, IHClassFactory hClassFactory)
{
    public bool TryCreate(IPlayer player, [NotNullWhen(true)] out IHuman? human)
    {
        human = null;

        var classId = playerRepository.GetHClassId(player);
        var hClass = hClassFactory.CreateOrDefault(classId);

        human = Human.Create(core, player, hClass);

        return true;
    }

    public bool TryCreateSurvivor(IPlayer player, [NotNullWhen(true)] out IHuman? survivor)
    {
        survivor = null;

        if (!player.IsValid)
        {
            return false;
        }

        var survivorClass = hClassFactory.Create<HSurvivor>();

        survivor = Human.Create(core, player, survivorClass);

        return true;
    }
}