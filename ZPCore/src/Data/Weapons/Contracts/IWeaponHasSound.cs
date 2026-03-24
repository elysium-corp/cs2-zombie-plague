using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Weapons.Contracts;

internal interface IWeaponHasSound
{
    string WeaponFireSound { get; }
    
    string WeaponFireOnEmpty { get; }
    
    string WeaponReload { get; }
    
    string WeaponZoom { get; }

    void OnWeaponFireSound(IPlayer player);

    void OnWeaponFireOnEmpty(IPlayer player);

    void OnWeaponReload(IPlayer player);
    
    void OnWeaponZoom(IPlayer player);
}