using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Core.Data.Service.Contracts;

namespace ZombiePlague.Core.Data.Service;

internal interface IMapService : IService;

internal sealed class MapService(ISwiftlyCore core) : IMapService
{
    public void Register()
    {
        core.Event.OnMapLoad += OnMapLoad;
    }

    public void Unregister()
    {
        core.Event.OnMapLoad -= OnMapLoad;
    }

    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        core.Scheduler.NextTick(RemoveAllBuyZones);
    }

    private void RemoveAllBuyZones()
    {
        var buyZones = core.EntitySystem
            .GetAllEntitiesByDesignerName<CBaseEntity>("func_buyzone")
            .ToList();

        foreach (var buyZone in buyZones)
        {
            if (buyZone.IsValid) buyZone.Despawn();
        }
    }
}