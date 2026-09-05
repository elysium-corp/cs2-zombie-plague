using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.DatabaseWeapons;
using CustomEquipment.Data.Equipments.Models;
using CustomEquipment.Database.Entities;
using CustomEquipment.Utils;
using Microsoft.EntityFrameworkCore;

namespace CustomEquipment.Database;

internal sealed class WeaponCatalogRepository(
    IDbContextFactory<CustomEquipmentDbContext> contextFactory
) : IWeaponCatalogRepository
{
    public IReadOnlyCollection<DatabaseWeaponItem> GetEnabledWeapons()
    {
        using var context = contextFactory.CreateDbContext();

        var entities = context.Weapons
            .AsNoTracking()
            .Where(weapon => weapon.Enabled)
            .Include(weapon => weapon.Sounds.Where(sound => sound.Enabled))
            .OrderBy(weapon => weapon.SortOrder)
            .ThenBy(weapon => weapon.Id)
            .AsSplitQuery()
            .ToArray();

        var weapons = new List<DatabaseWeaponItem>(entities.Length);

        foreach (var entity in entities)
        {
            try
            {
                weapons.Add(Map(entity));
            }
            catch (Exception exception)
            {
                throw new InvalidOperationException(
                    $"Custom equipment weapon '{entity.InternalName}' ({entity.Id}) is invalid.",
                    exception
                );
            }
        }

        return weapons;
    }

    private static DatabaseWeaponItem Map(WeaponEntity entity)
    {
        var slot = ParseEnum<Slot>(entity.Slot, nameof(entity.Slot));
        var weaponType = ParseEnum<WeaponType>(entity.WeaponType, nameof(entity.WeaponType));
        var rarity = ParseEnum<ItemRarity>(entity.Rarity, nameof(entity.Rarity));
        var accessFlags = (AccessFlags)entity.AccessFlags;

        if ((accessFlags & ~AccessFlags.All) != 0)
        {
            throw new InvalidOperationException($"Unknown access flags value '{entity.AccessFlags}'.");
        }

        if (entity.CycleTimeSecondary.HasValue && !entity.CycleTimePrimary.HasValue)
        {
            throw new InvalidOperationException("Secondary cycle time requires primary cycle time.");
        }

        var definition = new DatabaseWeaponDefinition(
            InheritorName: Required(entity.InheritorName, nameof(entity.InheritorName)),
            AccessFlags: accessFlags,
            DisplayName: Required(entity.DisplayName, nameof(entity.DisplayName)),
            DisplayNameKey: LocalizationKeyValidator.Required(
                entity.DisplayNameKey,
                nameof(entity.DisplayNameKey)
            ),
            InternalName: Required(entity.InternalName, nameof(entity.InternalName)),
            SubclassName: Required(entity.SubclassName, nameof(entity.SubclassName)),
            Slot: slot,
            WeaponType: weaponType,
            Model: Required(entity.Model, nameof(entity.Model)),
            WeaponDamage: MapDamage(entity),
            WeaponTiming: MapTiming(entity),
            Particle: MapParticle(entity),
            Ammunition: MapAmmunition(entity),
            Sounds: entity.Sounds
                .OrderBy(sound => sound.SortOrder)
                .ThenBy(sound => sound.Id)
                .Select(MapSound)
                .ToArray(),
            Rarity: rarity
        );

        return new DatabaseWeaponItem(definition);
    }

    internal static WeaponSound MapSound(WeaponSoundEntity entity)
    {
        var trigger = Required(entity.Trigger, nameof(entity.Trigger)).ToLowerInvariant();
        var eventName = Required(entity.EventName, nameof(entity.EventName));
        var replacesEventName = NullIfWhiteSpace(entity.ReplacesEventName);

        if (!IsSupportedTrigger(trigger))
        {
            throw new InvalidOperationException($"Unsupported sound trigger '{trigger}'.");
        }

        if (!IsSoundEventName(eventName))
        {
            throw new InvalidOperationException($"Sound event name '{eventName}' is invalid.");
        }

        if (replacesEventName is not null && !IsSoundEventName(replacesEventName))
        {
            throw new InvalidOperationException($"Replacement sound event name '{replacesEventName}' is invalid.");
        }

        if (string.Equals(eventName, replacesEventName, StringComparison.OrdinalIgnoreCase))
        {
            replacesEventName = null;
        }

        if (!float.IsFinite(entity.Volume) || entity.Volume is < 0 or > 10)
        {
            throw new InvalidOperationException($"Sound '{eventName}' has invalid volume (expected 0–10).");
        }

        return new WeaponSound
        {
            Trigger = trigger,
            EventName = eventName,
            ReplacesEventName = replacesEventName,
            Volume = entity.Volume
        };
    }

    private static bool IsSupportedTrigger(string trigger)
    {
        return trigger is WeaponSoundTriggers.Fire
            or WeaponSoundTriggers.Reload
            or WeaponSoundTriggers.Empty
            or WeaponSoundTriggers.Draw
            or WeaponSoundTriggers.Inspect
            or WeaponSoundTriggers.Zoom
            or WeaponSoundTriggers.SilencerOn
            or WeaponSoundTriggers.SilencerOff;
    }

    private static bool IsSoundEventName(string value)
    {
        if (value.Length is < 2 or > 256 || !IsAsciiLetter(value[0]))
        {
            return false;
        }

        return value.All(character =>
            IsAsciiLetter(character) ||
            char.IsAsciiDigit(character) ||
            character is '_' or '.'
        );
    }

    private static bool IsAsciiLetter(char value)
    {
        return value is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
    }

    private static WeaponDamage? MapDamage(WeaponEntity entity)
    {
        var hasDamage = entity.NumBullets.HasValue ||
                        entity.Penetration.HasValue ||
                        entity.EffectiveRange.HasValue ||
                        entity.RangeModifier.HasValue ||
                        HasDamageMultiplier(entity);

        if (!hasDamage)
        {
            return null;
        }

        return new WeaponDamage
        {
            DamageMultiplier = HasDamageMultiplier(entity)
                ? new DamageMultiplier
                {
                    Head = entity.DamageHead ?? 1.0f,
                    Chest = entity.DamageChest ?? 1.0f,
                    Stomach = entity.DamageStomach ?? 1.0f,
                    Arms = new DamageMultiplier.Arm(
                        entity.DamageLeftArm ?? 1.0f,
                        entity.DamageRightArm ?? 1.0f
                    ),
                    Legs = new DamageMultiplier.Leg(
                        entity.DamageLeftLeg ?? 1.0f,
                        entity.DamageRightLeg ?? 1.0f
                    ),
                    Neck = entity.DamageNeck ?? 1.0f
                }
                : null,
            NumBullets = entity.NumBullets ?? 1,
            Penetration = entity.Penetration,
            Range = entity.EffectiveRange,
            RangeModifier = entity.RangeModifier
        };
    }

    private static bool HasDamageMultiplier(WeaponEntity entity)
    {
        return entity.DamageHead.HasValue ||
               entity.DamageChest.HasValue ||
               entity.DamageStomach.HasValue ||
               entity.DamageLeftArm.HasValue ||
               entity.DamageRightArm.HasValue ||
               entity.DamageLeftLeg.HasValue ||
               entity.DamageRightLeg.HasValue ||
               entity.DamageNeck.HasValue;
    }

    private static WeaponTiming? MapTiming(WeaponEntity entity)
    {
        if (!entity.CycleTimePrimary.HasValue && !entity.DeployDuration.HasValue)
        {
            return null;
        }

        var cycleTime = new List<float>(2);

        if (entity.CycleTimePrimary.HasValue)
        {
            cycleTime.Add(entity.CycleTimePrimary.Value);
        }

        if (entity.CycleTimeSecondary.HasValue)
        {
            cycleTime.Add(entity.CycleTimeSecondary.Value);
        }

        return new WeaponTiming
        {
            CycleTime = cycleTime,
            DeployDuration = entity.DeployDuration
        };
    }

    private static WeaponParticle? MapParticle(WeaponEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.ParticleTracer) &&
            string.IsNullOrWhiteSpace(entity.ParticleImpact) &&
            string.IsNullOrWhiteSpace(entity.ParticleMuzzleFlash))
        {
            return null;
        }

        return new WeaponParticle
        {
            Trace = entity.ParticleTracer ?? string.Empty,
            Impact = entity.ParticleImpact ?? string.Empty,
            MuzzleFlash = entity.ParticleMuzzleFlash ?? string.Empty
        };
    }

    private static Ammunition? MapAmmunition(WeaponEntity entity)
    {
        if (!entity.ClipSize.HasValue && !entity.ReserveAmmo.HasValue)
        {
            return null;
        }

        return new Ammunition
        {
            Clip = entity.ClipSize,
            ReserveAmmo = entity.ReserveAmmo
        };
    }

    private static TEnum ParseEnum<TEnum>(string value, string field) where TEnum : struct, Enum
    {
        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var result) || !Enum.IsDefined(result))
        {
            throw new InvalidOperationException($"Unknown {field} value '{value}'.");
        }

        return result;
    }

    private static string Required(string? value, string field)
    {
        return string.IsNullOrWhiteSpace(value)
            ? throw new InvalidOperationException($"{field} cannot be empty.")
            : value.Trim();
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
