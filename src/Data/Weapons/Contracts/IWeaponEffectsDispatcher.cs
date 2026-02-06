using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace CS2ZombiePlague.Data.Weapons.Contracts;

public interface IWeaponEffectsDispatcher
{
    HookResult OnWeaponFirePost(EventWeaponFire @event);
    HookResult OnBulletImpactPost(EventBulletImpact @event);
    HookResult OnWeaponReloadPost(EventWeaponReload @event);
    HookResult OnWeaponZoomPost(EventWeaponZoom @event);
    HookResult OnWeaponFireOnEmptyPost(EventWeaponFireOnEmpty @event);
}