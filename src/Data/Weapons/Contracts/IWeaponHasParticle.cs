using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Weapons.Contracts;

public interface IWeaponHasParticle
{
    string WeaponFireParticle { get; }
    
    WeaponFireParticleType WeaponFireParticleType { get; }

    void OnWeaponFireParticle(IPlayer player, Vector? impactPos = null);
}