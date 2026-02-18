using CS2ZombiePlague.Data.Effects;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Weapons.Grenades;

public class FireNade(ISwiftlyCore core, CommonUtils commonUtils) : ICustomWeapon
{
    private readonly EffectManager _effectManager = DependencyManager.GetService<EffectManager>();
    public string OriginalName => "incgrenade_projectile";
    public string IternalName => "weapon_fire_nade";
    public string DisplayName => "FireNade";
    public void Load()
    {
        core.Event.OnEntityTakeDamage += OnEntityTakeDamage;
    }

    private void OnEntityTakeDamage(IOnEntityTakeDamageEvent @event)
    {
        var attacker = commonUtils.ResolvePlayerFromHandle(@event.Info.Attacker);
        
        if (attacker is not { IsValid: true } || !attacker.IsAlive || attacker.IsInfected()) return;
        
        var victim = commonUtils.FindPlayerByPawnAddress(@event.Entity.Address);
        
        if (victim is not { IsValid: true } || !victim.IsAlive) return;

        if (victim.IsHuman())
        {
            @event.Info.Damage = 0;
            return;
        }
        
        if(@event.Info.DamageType != DamageTypes_t.DMG_BURN) return;
        
        if(victim.IsBurn()) return;
        
        _effectManager.ApplyEffect<Burn>(attacker, victim);
    }
}