using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Utils.Extensions;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Weapons.Controller;

public class DamageModifier(IPlayer owner, IPlayerInventory inventory) : IDamageModifier
{
    public void OnEntityTakeDamage(IOnEntityTakeDamageEvent @event)
    {
        var attacker = @event.Info.Attacker.ResolvePlayerFromHandle();
        var victim = @event.Entity.Address.FindPlayerByPawnAddress();

        if (attacker is not { IsValid: true } || !attacker.IsAlive) return;
        
        if (attacker.PlayerID != owner.PlayerID) return;

        if (victim is not { IsValid: true } || !victim.IsAlive) return;
        
        if (victim.PlayerID == attacker.PlayerID) return;

        var tryGetWeapon = inventory.TryGetActiveWeapon(out var weapon);
        
        if (tryGetWeapon && weapon != null)
        {
            var baseDamage = @event.Info.Damage;
            @event.Info.Damage = baseDamage * weapon.DamageMultiplier;
        }
    }
}