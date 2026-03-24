using System.Diagnostics.CodeAnalysis;
using ZPCore.Data.Weapons.Contracts;
using ZPCore.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Weapons.Controller;

internal class WeaponEffectsDispatcher(IPlayer owner, IPlayerInventory inventory) : IWeaponEffectsDispatcher
{
    private readonly ISwiftlyCore core = DependencyManager.GetService<ISwiftlyCore>();
    
    public HookResult OnBulletImpactPost(EventBulletImpact @event)
    {
        var attacker = @event.UserIdPlayer;
        
        if (!IsOwner(attacker)) return HookResult.Continue;

        if (inventory.TryGetActiveWeapon(out var weapon) && weapon.HasWeaponFireParticle())
        {
            var impactPos = new Vector(@event.X, @event.Y, @event.Z);
            weapon.OnWeaponFireParticle(attacker, impactPos);
        }

        return HookResult.Continue;
    }

    public HookResult OnWeaponFirePost(EventWeaponFire @event)
    {
        var attacker = @event.UserIdPlayer;
        if (!IsOwner(attacker)) return HookResult.Continue;

        if (inventory.TryGetActiveWeapon(out var weapon) && weapon.HasWeaponFireSound())
            weapon.OnWeaponFireSound(attacker);

        return HookResult.Continue;
    }

    public HookResult OnWeaponFireOnEmptyPost(EventWeaponFireOnEmpty @event)
    {
        var attacker = @event.UserIdPlayer;
        if (!IsOwner(attacker)) return HookResult.Continue;
        
        if (inventory.TryGetActiveWeapon(out var weapon) && weapon.HasWeaponFireOnEmpty())
            weapon.OnWeaponFireOnEmpty(attacker);

        return HookResult.Continue;
    }

    public HookResult OnWeaponReloadPost(EventWeaponReload @event)
    {
        var attacker = @event.UserIdPlayer;
        if (!IsOwner(attacker)) return HookResult.Continue;

        if (inventory.TryGetActiveWeapon(out var weapon) && weapon.HasWeaponReload())
            weapon.OnWeaponReload(attacker);

        return HookResult.Continue;
    }

    public HookResult OnWeaponZoomPost(EventWeaponZoom @event)
    {
        var attacker = @event.UserIdPlayer;
        if (!IsOwner(attacker)) return HookResult.Continue;

        if (inventory.TryGetActiveWeapon(out var weapon) && weapon.HasWeaponZoom())
            weapon.OnWeaponZoom(attacker);

        return HookResult.Continue;
    }

    private bool IsOwner([NotNullWhen(true)]IPlayer? player)
    {
        return player?.PlayerID == owner.PlayerID;
    }
}