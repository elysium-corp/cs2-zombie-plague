using CS2ZombiePlague.Di;
using CS2ZombiePlague.Service;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Weapons.Controller;

public sealed class WeaponController : IWeaponController
{
    private readonly ISwiftlyCore _core;

    private readonly PlayerInventory _inventory;
    private readonly WeaponEffectsDispatcher _weaponEffects;
    private readonly DamageModifier _damageModifier;
    
    private readonly Guid _guidOnWeaponFireOnEmptyPost;
    private readonly Guid _guidOnBulletImpactPost;
    private readonly Guid _guidOnWeaponReloadPost;
    private readonly Guid _guidOnWeaponFirePost;
    private readonly Guid _guidOnWeaponZoomPost;

    public WeaponController(IPlayer player)
    {
        _core = DependencyManager.GetService<ISwiftlyCore>();
        var commonUtils = DependencyManager.GetService<CommonUtils>();
        var weaponService = DependencyManager.GetService<WeaponService>();

        _inventory = new PlayerInventory(player, weaponService);
        _weaponEffects = new WeaponEffectsDispatcher(player, _inventory);
        _damageModifier = new DamageModifier(player, _inventory, commonUtils);
        
        _guidOnWeaponFireOnEmptyPost = _core.GameEvent.HookPost<EventWeaponFireOnEmpty>(OnWeaponFireOnEmptyPost);
        _guidOnBulletImpactPost = _core.GameEvent.HookPost<EventBulletImpact>(OnBulletImpactPost);
        _guidOnWeaponReloadPost = _core.GameEvent.HookPost<EventWeaponReload>(OnWeaponReloadPost);
        _guidOnWeaponFirePost = _core.GameEvent.HookPost<EventWeaponFire>(OnWeaponFirePost);
        _guidOnWeaponZoomPost = _core.GameEvent.HookPost<EventWeaponZoom>(OnWeaponZoomPost);

        _core.Event.OnWeaponServicesDropWeaponHook += OnWeaponServicesDropWeaponHook;
        _core.Event.OnWeaponServicesCanUseHook += OnWeaponServicesCanUseHook;
        _core.Event.OnEntityTakeDamage += OnEntityTakeDamage;
    }

    private void OnWeaponServicesCanUseHook(IOnWeaponServicesCanUseHookEvent @event) =>
        _inventory.OnCanUseHook(@event);

    private void OnWeaponServicesDropWeaponHook(IOnWeaponServicesDropWeaponHook @event) =>
        _inventory.OnDropHook(@event);

    private HookResult OnBulletImpactPost(EventBulletImpact @event) =>
        _weaponEffects.OnBulletImpactPost(@event);

    private HookResult OnWeaponFirePost(EventWeaponFire @event) =>
        _weaponEffects.OnWeaponFirePost(@event);

    private HookResult OnWeaponFireOnEmptyPost(EventWeaponFireOnEmpty @event) =>
        _weaponEffects.OnWeaponFireOnEmptyPost(@event);

    private HookResult OnWeaponReloadPost(EventWeaponReload @event) =>
        _weaponEffects.OnWeaponReloadPost(@event);

    private HookResult OnWeaponZoomPost(EventWeaponZoom @event) =>
        _weaponEffects.OnWeaponZoomPost(@event);

    private void OnEntityTakeDamage(IOnEntityTakeDamageEvent @event) =>
        _damageModifier.OnEntityTakeDamage(@event);

    public void Dispose()
    {
        _core.GameEvent.Unhook(_guidOnWeaponFireOnEmptyPost);
        _core.GameEvent.Unhook(_guidOnWeaponReloadPost);
        _core.GameEvent.Unhook(_guidOnBulletImpactPost);
        _core.GameEvent.Unhook(_guidOnWeaponFirePost);
        _core.GameEvent.Unhook(_guidOnWeaponZoomPost);
        
        _core.Event.OnEntityTakeDamage -= OnEntityTakeDamage;
        _core.Event.OnWeaponServicesCanUseHook -= OnWeaponServicesCanUseHook;
        _core.Event.OnWeaponServicesDropWeaponHook -= OnWeaponServicesDropWeaponHook;
    }
}