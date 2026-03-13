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
    private readonly GrenadeHandler _grenadeHandler;
    
    private readonly Guid _guidOnWeaponFireOnEmptyPost;
    private readonly Guid _guidOnBulletImpactPost;
    private readonly Guid _guidOnWeaponReloadPost;
    private readonly Guid _guidOnWeaponFirePost;
    private readonly Guid _guidOnWeaponZoomPost;

    private readonly Guid _guidOnGrenadeThrownPre;
    private readonly Guid _guidOnDecoyStartedPre;
    private readonly Guid _guidOnHegrenadeDetonatePre;
    private readonly Guid _guidOnMolotovDetonatePre;
    private readonly Guid _guidOnSmokegrenadeDetonatePre;

    public WeaponController(IPlayer player)
    {
        _core = DependencyManager.GetService<ISwiftlyCore>();
        var commonUtils = DependencyManager.GetService<CommonUtils>();
        var weaponService = DependencyManager.GetService<WeaponService>();

        _inventory = new PlayerInventory(player, weaponService);
        _weaponEffects = new WeaponEffectsDispatcher(player, _inventory);
        _damageModifier = new DamageModifier(player, _inventory, commonUtils);
        _grenadeHandler = new GrenadeHandler(player, _core, weaponService);
        
        _guidOnWeaponFireOnEmptyPost = _core.GameEvent.HookPost<EventWeaponFireOnEmpty>(OnWeaponFireOnEmptyPost);
        _guidOnBulletImpactPost = _core.GameEvent.HookPost<EventBulletImpact>(OnBulletImpactPost);
        _guidOnWeaponReloadPost = _core.GameEvent.HookPost<EventWeaponReload>(OnWeaponReloadPost);
        _guidOnWeaponFirePost = _core.GameEvent.HookPost<EventWeaponFire>(OnWeaponFirePost);
        _guidOnWeaponZoomPost = _core.GameEvent.HookPost<EventWeaponZoom>(OnWeaponZoomPost);

        _guidOnGrenadeThrownPre = _core.GameEvent.HookPre<EventGrenadeThrown>(OnGrenadeThrownPre);
        _guidOnDecoyStartedPre = _core.GameEvent.HookPre<EventDecoyStarted>(OnDecoyStartedPre);
        _guidOnHegrenadeDetonatePre = _core.GameEvent.HookPre<EventHegrenadeDetonate>(OnHegrenadeDetonatePre);
        _guidOnMolotovDetonatePre = _core.GameEvent.HookPre<EventMolotovDetonate>(OnMolotovDetonatePre);
        _guidOnSmokegrenadeDetonatePre = _core.GameEvent.HookPre<EventSmokegrenadeDetonate>(OnSmokegrenadeDetonatePre);
        
        _core.Event.OnWeaponServicesDropWeaponHook += OnWeaponServicesDropWeaponHook;
        _core.Event.OnWeaponServicesCanUseHook += OnWeaponServicesCanUseHook;
        _core.Event.OnEntityTakeDamage += OnEntityTakeDamage;
    }

    private void OnWeaponServicesCanUseHook(IOnWeaponServicesCanUseHookEvent gameEvent) =>
        _inventory.OnCanUseHook(gameEvent);

    private void OnWeaponServicesDropWeaponHook(IOnWeaponServicesDropWeaponHook gameEvent) =>
        _inventory.OnDropHook(gameEvent);

    private HookResult OnBulletImpactPost(EventBulletImpact gameEvent) =>
        _weaponEffects.OnBulletImpactPost(gameEvent);

    private HookResult OnWeaponFirePost(EventWeaponFire gameEvent) =>
        _weaponEffects.OnWeaponFirePost(gameEvent);

    private HookResult OnWeaponFireOnEmptyPost(EventWeaponFireOnEmpty gameEvent) =>
        _weaponEffects.OnWeaponFireOnEmptyPost(gameEvent);

    private HookResult OnWeaponReloadPost(EventWeaponReload gameEvent) =>
        _weaponEffects.OnWeaponReloadPost(gameEvent);

    private HookResult OnWeaponZoomPost(EventWeaponZoom gameEvent) =>
        _weaponEffects.OnWeaponZoomPost(gameEvent);

    private void OnEntityTakeDamage(IOnEntityTakeDamageEvent gameEvent) =>
        _damageModifier.OnEntityTakeDamage(gameEvent);

    private HookResult OnGrenadeThrownPre(EventGrenadeThrown gameEvent) =>
        _grenadeHandler.OnGrenadeThrownPre(gameEvent);
    
    private HookResult OnDecoyStartedPre(EventDecoyStarted gameEvent) =>
        _grenadeHandler.OnDecoyStartedPre(gameEvent);
    
    private HookResult OnHegrenadeDetonatePre(EventHegrenadeDetonate gameEvent) =>
        _grenadeHandler.OnHegrenadeDetonatePre(gameEvent);

    private HookResult OnMolotovDetonatePre(EventMolotovDetonate gameEvent) =>
        _grenadeHandler.OnMolotovDetonatePre(gameEvent);
    
    private HookResult OnSmokegrenadeDetonatePre(EventSmokegrenadeDetonate gameEvent) =>
        _grenadeHandler.OnSmokegrenadeDetonatePre(gameEvent);

    public void Dispose()
    {
        _core.GameEvent.Unhook(_guidOnWeaponFireOnEmptyPost);
        _core.GameEvent.Unhook(_guidOnWeaponReloadPost);
        _core.GameEvent.Unhook(_guidOnBulletImpactPost);
        _core.GameEvent.Unhook(_guidOnWeaponFirePost);
        _core.GameEvent.Unhook(_guidOnWeaponZoomPost);
        
        _core.GameEvent.Unhook(_guidOnGrenadeThrownPre);
        _core.GameEvent.Unhook(_guidOnDecoyStartedPre);
        _core.GameEvent.Unhook(_guidOnHegrenadeDetonatePre);
        _core.GameEvent.Unhook(_guidOnMolotovDetonatePre);
        
        _core.Event.OnEntityTakeDamage -= OnEntityTakeDamage;
        _core.Event.OnWeaponServicesCanUseHook -= OnWeaponServicesCanUseHook;
        _core.Event.OnWeaponServicesDropWeaponHook -= OnWeaponServicesDropWeaponHook;
    }
}