using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Service.Contracts;

namespace ZombiePlague.Core.Data.Service;

internal interface IInfectionService : IService;

internal sealed class InfectionService(
    ISwiftlyCore core,
    IPlayerManager playerManager
) : IInfectionService
{
    public void Register()
    {
        core.GameHooks.Items.CanAcquire.Pre += OnCanAcquire;
        core.GameHooks.Weapons.CanUse.Pre += OnCanUse;
        core.GameHooks.Weapons.Drop.Pre += OnDrop;
    }

    public void Unregister()
    {
        core.GameHooks.Items.CanAcquire.Pre -= OnCanAcquire;
        core.GameHooks.Weapons.CanUse.Pre -= OnCanUse;
        core.GameHooks.Weapons.Drop.Pre -= OnDrop;
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

    private static bool IsAllowedForZombie(string? weaponName)
    {
        return weaponName?.Contains("knife", StringComparison.OrdinalIgnoreCase) == true ||
               weaponName?.Contains("smoke", StringComparison.OrdinalIgnoreCase) == true;
    }
}