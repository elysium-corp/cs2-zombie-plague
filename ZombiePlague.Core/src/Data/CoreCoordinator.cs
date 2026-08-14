using ZombiePlague.Core.Data.Entities.Registrator;
using ZombiePlague.Core.Data.Rounds.Registrator;
using ZombiePlague.Core.Data.Service;
using ZombiePlague.Core.Data.Service.Contracts;

namespace ZombiePlague.Core.Data;

internal sealed class CoreCoordinator(
    IPlayerService playerService,
    IRoundService roundService,
    IInfectionService infectionService,
    IKnockbackService knockbackService,
    IMapService mapService,
    IRoundRegistrator roundRegistrator,
    IZClassRegistrator zClassRegistrator,
    ICommandService commandService
) : ICoreCoordinator
{
    public void Start()
    {
        mapService.Register();
        roundRegistrator.Register();
        zClassRegistrator.Register();
        playerService.Register();
        roundService.Register();
        infectionService.Register();
        knockbackService.Register();
        commandService.Register();
    }

    public void Stop()
    {
        mapService.Unregister();
        knockbackService.Unregister();
        infectionService.Unregister();
        roundService.Unregister();
        commandService.Unregister();
        playerService.Unregister();
    }
}