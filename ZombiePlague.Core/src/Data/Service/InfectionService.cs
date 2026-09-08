using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Api.Events;
using ZombiePlague.Api.Events.Contexts.Player;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Service.Contracts;

namespace ZombiePlague.Core.Data.Service;

internal interface IInfectionService : IService;

internal sealed class InfectionService(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    IZombiePlagueEvents events
) : IInfectionService
{
    private bool _registered;
    private int _generation;

    public void Register()
    {
        if (_registered) return;
        _registered = true;
        _generation++;

        core.GameHooks.Items.CanAcquire.Pre += OnCanAcquire;
        core.GameHooks.Weapons.CanUse.Pre += OnCanUse;
        core.GameHooks.Weapons.Drop.Pre += OnDrop;
        
        events.Players.Infected.Hook(OnPlayerInfected);
        events.Players.RoleApplied.Hook(OnPlayerRoleApplied);
        events.Players.BecameNemesis.Hook(OnPlayerBecameNemesis);
    }

    public void Unregister()
    {
        if (!_registered) return;
        _registered = false;
        _generation++;

        core.GameHooks.Items.CanAcquire.Pre -= OnCanAcquire;
        core.GameHooks.Weapons.CanUse.Pre -= OnCanUse;
        core.GameHooks.Weapons.Drop.Pre -= OnDrop;
        
        events.Players.Infected.Unhook(OnPlayerInfected);
        events.Players.RoleApplied.Unhook(OnPlayerRoleApplied);
        events.Players.BecameNemesis.Unhook(OnPlayerBecameNemesis);
    }
    
    private void OnCanAcquire(ref CanAcquireItemPreContext context)
    {
        var player = context.Params.Player;

        if (!player.IsValid || !playerManager.IsZombie(player))
        {
            return;
        }

        var weaponName = context.Params.WeaponVData?.Name.Value;

        if (IsAllowedForZombie(weaponName))
        {
            return;
        }

        context.SetReturn(AcquireResult.NotAllowedByProhibition);
        context.SetHookResult(HookResult.Stop);
    }

    private void OnCanUse(ref CanUseWeaponPreContext context)
    {
        var player = context.Params.Player;

        if (!player.IsValid || !playerManager.IsZombie(player))
        {
            return;
        }

        if (!IsAllowedForZombie(context.Params.Weapon.DesignerName))
        {
            context.SetReturn(false);
            context.SetHookResult(HookResult.Stop);
        }
    }

    private void OnDrop(ref WeaponDropPreContext context)
    {
        var player = context.Params.Player;

        if (!player.IsValid || !playerManager.IsZombie(player))
        {
            return;
        }

        context.SetHookResult(HookResult.Stop);
    }

    private void OnPlayerInfected(ref PlayerInfectedContext context)
    {
        ScheduleCosmeticsReset(context.Player);
    }

    private void OnPlayerRoleApplied(ref PlayerRoleAppliedContext context)
    {
        ScheduleCosmeticsReset(context.Player);
    }

    private void OnPlayerBecameNemesis(ref PlayerBecameNemesisContext context)
    {
        ScheduleCosmeticsReset(context.Player);
    }

    private void ScheduleCosmeticsReset(IPlayer player)
    {
        if (!_registered || !player.IsValid || !player.IsAlive || !playerManager.IsZombie(player) ||
            player.PlayerPawn is not { IsValid: true } pawn)
        {
            return;
        }

        var sessionId = player.SessionId;
        var pawnAddress = pawn.Address;
        var generation = _generation;

        // Выполняем после отложенного применения модели роли, в том числе при возрождении.
        core.Scheduler.NextWorldUpdate(() =>
        {
            if (!_registered || generation != _generation) return;

            var currentPlayer = core.PlayerManager.GetPlayerFromSessionId(sessionId);
            if (currentPlayer is not { IsValid: true, IsAlive: true } ||
                !playerManager.IsZombie(currentPlayer) ||
                currentPlayer.PlayerPawn is not { IsValid: true } currentPawn ||
                currentPawn.Address != pawnAddress)
            {
                return;
            }

            RemoveGloves(currentPlayer);
            ResetKnife(currentPawn);
        });
    }

    private static bool IsAllowedForZombie(string? weaponName)
    {
        return weaponName?.Contains("knife", StringComparison.OrdinalIgnoreCase) == true ||
               weaponName?.Contains("smoke", StringComparison.OrdinalIgnoreCase) == true ||
               weaponName?.Contains("hegrenade", StringComparison.OrdinalIgnoreCase) == true;
    }
    
    private static void RemoveGloves(IPlayer player)
    {
        if (!player.IsValid || !player.IsAlive) return;

        var pawn = player.PlayerPawn;

        if (pawn is null || !pawn.IsValid)
        {
            return;
        }

        var gloves = pawn.EconGloves;

        gloves.AttributeList.Attributes.RemoveAll();
        gloves.NetworkedDynamicAttributes.Attributes.RemoveAll();

        gloves.ItemDefinitionIndex = 0;
        gloves.ItemID = 0;
        gloves.ItemIDHigh = 0;
        gloves.ItemIDLow = 0;
        gloves.AccountID = 0;
        gloves.InventoryPosition = 0;
        gloves.Initialized = false;

        gloves.ItemDefinitionIndexUpdated();
        gloves.ItemIDHighUpdated();
        gloves.ItemIDLowUpdated();
        gloves.AccountIDUpdated();
        gloves.InventoryPositionUpdated();
        gloves.InitializedUpdated();
        pawn.EconGlovesUpdated();
        pawn.EconGlovesChanged++;
        pawn.EconGlovesChangedUpdated();

        _ = pawn.AcceptInputAsync("SetBodygroup", value: "first_or_third_person,0");
    }

    private static void ResetKnife(CCSPlayerPawn pawn)
    {
        if (pawn.WeaponServices is not { } weapons) return;

        foreach (var weapon in weapons.MyValidWeapons.Where(weapon =>
                     weapon.DesignerName.Contains("knife", StringComparison.OrdinalIgnoreCase)))
        {
            var item = weapon.AttributeManager.Item;
            item.AttributeList.Attributes.RemoveAll();
            item.NetworkedDynamicAttributes.Attributes.RemoveAll();
            item.ItemDefinitionIndex = 59;
            item.ItemID = 0;
            item.ItemIDHigh = 0;
            item.ItemIDLow = 0;
            item.AccountID = 0;
            item.CustomName = "";
            item.CustomNameOverride = "";
            item.ItemDefinitionIndexUpdated();
            item.ItemIDHighUpdated();
            item.ItemIDLowUpdated();
            item.AccountIDUpdated();
            item.CustomNameUpdated();

            // Обычный нож стороны T, включая случай восстановления ножа из инвентаря Steam.
            weapon.AcceptInput("ChangeSubclass", "59");
            weapon.SetModel("weapons/models/knife/knife_t.vmdl");
        }
    }
}
