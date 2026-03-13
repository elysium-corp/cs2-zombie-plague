using CS2ZombiePlague.Data.Weapons.Enums;
using CS2ZombiePlague.Di;
using CS2ZombiePlague.Service;
using CS2ZombiePlague.Utils.Extensions;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;
using static CS2ZombiePlague.Data.Weapons.Exceptions.WeaponException;

namespace CS2ZombiePlague.Data.Weapons.Contracts;

public abstract class BaseWeapon : IWeapon, IWeaponHasParticle, IWeaponHasSound 
{
    private readonly WeaponParticleService _weaponParticleService = DependencyManager.GetService<WeaponParticleService>();

    public virtual CCSWeaponBase AttachedWeapon
    {
        get => field ?? throw new NotAttachedWeaponException();
        set;
    }

    public abstract string InheritorName { get; }

    public abstract string DisplayName { get; }

    public abstract string InternalName { get; }

    public abstract WeaponSlot Slot { get; }
    
    public abstract string Model { get; }

    public abstract WeaponRarity WeaponRarity { get; }

    public virtual float DamageMultiplier => 1.0f;

    public virtual string WeaponFireParticle => "";

    public virtual WeaponFireParticleType WeaponFireParticleType => WeaponFireParticleType.Single;

    public virtual void OnWeaponFireParticle(IPlayer player, Vector? impactPos = null)
    {
        _weaponParticleService.OnWeaponFireParticle(player, WeaponFireParticle, WeaponFireParticleType, impactPos);
    }
    
    public virtual string WeaponFireSound => "";
    
    public virtual string WeaponFireOnEmpty => "";
    
    public virtual string WeaponReload => "";
    
    public virtual string WeaponZoom => "";

    public virtual void OnWeaponFireSound(IPlayer player)
    {
        EmitSound(WeaponFireSound);
    }

    public virtual void OnWeaponFireOnEmpty(IPlayer player)
    {
        EmitSound(WeaponFireOnEmpty);
    }

    public virtual void OnWeaponReload(IPlayer player)
    {
        EmitSound(WeaponReload);
    }

    public virtual void OnWeaponZoom(IPlayer player)
    {
        EmitSound(WeaponZoom);
    }

    public bool HasWeaponFireParticle()
    {
        return WeaponFireParticle.IsNotNullOrEmpty();
    }

    public bool HasWeaponFireSound()
    {
        return WeaponFireSound.IsNotNullOrEmpty();
    }

    public bool HasWeaponFireOnEmpty()
    {
        return WeaponFireOnEmpty.IsNotNullOrEmpty();
    }

    public bool HasWeaponReload()
    {
        return WeaponReload.IsNotNullOrEmpty();
    }

    public bool HasWeaponZoom()
    {
        return WeaponZoom.IsNotNullOrEmpty();
    }

    private void EmitSound(string soundEvent)
    {
        using var sound = new SoundEvent(soundEvent);

        sound.Recipients.AddAllPlayers();
        sound.SourceEntityIndex = (int)AttachedWeapon.Index;
        
        sound.Emit();
    }
}