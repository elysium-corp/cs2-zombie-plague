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
    public void Register()
    {
        core.GameHooks.Items.CanAcquire.Pre += OnCanAcquire;
        core.GameHooks.Weapons.CanUse.Pre += OnCanUse;
        core.GameHooks.Weapons.Drop.Pre += OnDrop;
        
        events.Players.Infected.Hook(OnPlayerInfected);
    }

    public void Unregister()
    {
        core.GameHooks.Items.CanAcquire.Pre -= OnCanAcquire;
        core.GameHooks.Weapons.CanUse.Pre -= OnCanUse;
        core.GameHooks.Weapons.Drop.Pre -= OnDrop;
        
        events.Players.Infected.Unhook(OnPlayerInfected);
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
        var player = context.Player;
        
        core.Scheduler.NextWorldUpdate(() =>
        {
            RemoveGloves(player);
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

        _ = pawn.AcceptInputAsync("SetBodygroup", value: "first_or_third_person,0");
    }
}
