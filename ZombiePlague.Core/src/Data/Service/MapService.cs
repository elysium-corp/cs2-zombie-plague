using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Service.Contracts;

namespace ZombiePlague.Core.Data.Service;

internal interface IMapService : IService;

internal sealed class MapService(ISwiftlyCore core, IPlayerManager playerManager) : IMapService
{
    private bool _registered;
    public void Register()
    {
        if (_registered) return;
        _registered = true;
        core.Event.OnMapLoad += OnMapLoad;
        core.Event.OnMapUnload += OnMapUnload;
    }

    public void Unregister()
    {
        if (!_registered) return;
        _registered = false;
        core.Event.OnMapLoad -= OnMapLoad;
        core.Event.OnMapUnload -= OnMapUnload;
        UnbindAllAbilities(force: true);
    }

    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        core.Scheduler.NextTick(RemoveAllBuyZones);
    }

    private void OnMapUnload(IOnMapUnloadEvent @event)
    {
        core.Scheduler.NextTick(() => UnbindAllAbilities());
    }

    private void RemoveAllBuyZones()
    {
        if (!_registered) return;
        var buyZones = core.EntitySystem
            .GetAllEntitiesByDesignerName<CBaseEntity>("func_buyzone")
            .ToList();

        foreach (var buyZone in buyZones)
        {
            if (buyZone.IsValid) buyZone.Despawn();
        }
    }

    private void UnbindAllAbilities(bool force = false)
    {
        if (!force && !_registered) return;
        var players = playerManager.GetAllPlayers();

        foreach (var player in players)
        {
            if (playerManager.TryGetRole(player, out var role))
            {
                role.Unbind();
            }
        }
    }
}
