using SwiftlyS2.Shared.Events;

namespace ZPCore.Data.Weapons.Contracts;

internal interface IDamageModifier
{
    void OnEntityTakeDamage(IOnEntityTakeDamageEvent @event);
}