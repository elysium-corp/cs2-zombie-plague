using SwiftlyS2.Shared.Events;

namespace CS2ZombiePlague.Data.Weapons.Contracts;

public interface IDamageModifier
{
    void OnEntityTakeDamage(IOnEntityTakeDamageEvent @event);
}