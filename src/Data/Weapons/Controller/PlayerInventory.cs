using System.Diagnostics.CodeAnalysis;
using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Di;
using CS2ZombiePlague.Service;
using CS2ZombiePlague.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Weapons.Controller;

public class PlayerInventory(IPlayer owner, WeaponService weaponService) : IPlayerInventory
{
    public List<BaseWeapon> Weapons { get; } = [];

    private readonly ISwiftlyCore _core = DependencyManager.GetService<ISwiftlyCore>();

    public void OnCanUseHook(IOnWeaponServicesCanUseHookEvent @event)
    {
        var player = @event.WeaponServices.Pawn.ToPlayer();
        var engineResult = @event.OriginalResult;
        
        if (!engineResult || IsNotSelf(player)) return;
        
        var index = @event.Weapon.Index;
        
        WeaponUpdateSafelyInNextTick(() =>
        {
            var weapon = weaponService.GetWeaponByIndex(index);
            
            if (weapon == null)
            {
                return;
            }
            
            Add(weapon);
        });
    }

    public void OnDropHook(IOnWeaponServicesDropWeaponHook @event)
    {
        var weaponServices = @event.WeaponServices;
        var player = weaponServices.Pawn.ToPlayer();

        if (IsNotSelf(player))
        {
            return;
        }

        WeaponUpdateSafelyInNextTick(() =>
        {
            var weaponIds = weaponServices.MyWeaponsAsIds();
            var weaponSnapshot = Weapons.ToList();

            foreach (var weapon in weaponSnapshot)
            {
                var foundWeaponIndex = weapon.AttachedWeapon.Index;

                if (!weaponIds.Contains((int)foundWeaponIndex))
                {
                    Weapons.Remove(weapon);
                }
            }
        });
    }
    
    public bool TryGetActiveWeapon([NotNullWhen(true)] out BaseWeapon? weapon)
    {
        weapon = null;
        var indexCurrentWeapon = (int?)owner.RequiredPlayerPawn.WeaponServices?.ActiveWeapon.Value?.Index;

        if (indexCurrentWeapon == null) return false;

        var activeWeapon = Weapons.Find(weapon => weapon.AttachedWeapon.Index == indexCurrentWeapon);

        if (activeWeapon == null) return false;
        
        weapon = activeWeapon;
        return true;
    }
    
    public bool Add(BaseWeapon weapon)
    {
        var idx = weapon.AttachedWeapon.Index;
        var foundWeapon = Weapons.Find(w => w.AttachedWeapon.Index == idx);

        if (foundWeapon != null) return false;
        
        Weapons.Add(weapon);
        return true;
    }
    
    private void WeaponUpdateSafelyInNextTick(Action action)
    {
        _core.Scheduler.NextTick(action);
    }

    private bool IsNotSelf([NotNullWhen(false)] IPlayer? player)
    {
        return !IsSelf(player);
    }

    private bool IsSelf([NotNullWhen(true)]IPlayer? player)
    {
        return player?.PlayerID == owner.PlayerID;
    }
}