using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Events;
using CustomEquipment.Data.Equipments.Weapons.Equipments;
using CustomEquipment.Services;
using CustomEquipment.Utils;
using Economy.Api;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Controllers;

internal sealed class MineController(
    ISwiftlyCore core,
    IEventSubscriber eventSubscriber,
    IEconomyApi economyApi,
    ILaserMineInstallerService laserMineInstallerService)
    : IMineController, IDisposable
{
    private readonly Dictionary<CBaseModelEntity, IPlayer> _mineOwners = [];

    public void Initialize()
    {
        eventSubscriber.OnItemBought += OnItemBought;
        eventSubscriber.OnMinePlaced += OnMinePlaced;
        core.GameHooks.Entities.TakeDamage.Pre += OnEntityTakeDamage;
    }

    public void Dispose()
    {
        eventSubscriber.OnItemBought -= OnItemBought;
        eventSubscriber.OnMinePlaced -= OnMinePlaced;
        core.GameHooks.Entities.TakeDamage.Pre -= OnEntityTakeDamage;
    }

    private void OnItemBought(IPlayer player, IShopItem item)
    {
        var laserMine = item as LaserMine;

        if (laserMine == null) return;

        UpdateNotValidMines();

        var playerPawn = player.PlayerPawn;

        if (playerPawn == null || !playerPawn.IsValid || !player.IsAlive) return;

        if (_mineOwners.ContainsValue(player))
        {
            core.PlayerManager.SendAlertAsync("У вас уже есть мина");

            economyApi.GiveMoney(player, item.Price.Item);

            return;
        }

        if (!laserMineInstallerService.TrySetup(player, laserMine))
        {
            core.PlayerManager.SendAlertAsync("Невозможно разместить");

            economyApi.GiveMoney(player, item.Price.Item);
        }
    }

    private void OnMinePlaced(IPlayer player, LaserMineEntityBase mine)
    {
        var entity = mine.LaserMine;

        if (entity == null) return;

        _mineOwners.Add(entity, player);
    }

    private void OnEntityTakeDamage(ref TakeDamageEntityPreContext hook)
    {
        var attacker = hook.Params.Info.Attacker.ResolvePlayerFromHandle();
        var victim = hook.Params.Entity as CBaseModelEntity;

        if (victim == null || attacker == null) return;

        if (!_mineOwners.ContainsKey(victim)) return;

        if (attacker.PlayerPawn?.Team != victim.Team) return;

        _mineOwners.TryGetValue(victim, out var player);

        if (!attacker.Equals(player))
        {
            hook.Params.Info.Damage = 0;
            return;
        }

        if (victim.Health - hook.Params.Info.Damage <= 0)
        {
            _mineOwners.Remove(victim);
        }
    }

    private void UpdateNotValidMines()
    {
        foreach (var pair in _mineOwners.ToArray())
        {
            if (!pair.Key.IsValidEntity)
            {
                _mineOwners.Remove(pair.Key);
            }
        }
    }
}