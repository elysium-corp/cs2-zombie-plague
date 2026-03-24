using System.Diagnostics.CodeAnalysis;
using SwiftlyS2.Shared.Events;

namespace ZPCore.Data.Weapons.Contracts;

internal interface IPlayerInventory
{
    List<BaseWeapon> Weapons { get; }
    
    void OnCanUseHook(IOnWeaponServicesCanUseHookEvent @event);
    
    void OnDropHook(IOnWeaponServicesDropWeaponHook @event);
    
    bool TryGetActiveWeapon([NotNullWhen(true)] out BaseWeapon? weapon);

    public bool Add(BaseWeapon weapon);
}