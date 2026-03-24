using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Weapons.Contracts;

internal interface IWeaponHasParticle
{
    string WeaponFireParticle { get; }
    
    WeaponFireParticleType WeaponFireParticleType { get; }

    void OnWeaponFireParticle(IPlayer player, Vector? impactPos = null);
}