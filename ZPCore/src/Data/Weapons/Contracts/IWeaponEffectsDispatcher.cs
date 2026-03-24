using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace ZPCore.Data.Weapons.Contracts;

internal interface IWeaponEffectsDispatcher
{
    HookResult OnWeaponFirePost(EventWeaponFire @event);
    HookResult OnBulletImpactPost(EventBulletImpact @event);
    HookResult OnWeaponReloadPost(EventWeaponReload @event);
    HookResult OnWeaponZoomPost(EventWeaponZoom @event);
    HookResult OnWeaponFireOnEmptyPost(EventWeaponFireOnEmpty @event);
}