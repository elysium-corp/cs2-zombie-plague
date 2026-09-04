using System.Text.Json;
using System.Text.Json.Serialization;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Database.Entities;
using CustomEquipment.Utils;
using Microsoft.EntityFrameworkCore;

namespace CustomEquipment.Database;

internal sealed class GameplayItemCatalogRepository(
    IDbContextFactory<CustomEquipmentDbContext> contextFactory
) : IGameplayItemCatalogRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public IReadOnlyCollection<GameplayItemDefinition> GetItems()
    {
        using var context = contextFactory.CreateDbContext();

        var items = context.GameplayItems
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .AsEnumerable()
            .Select(Map)
            .ToArray();

        var configuredKeys = items
            .Select(item => item.ImplementationKey)
            .ToHashSet(StringComparer.Ordinal);
        var missingKeys = GameplayItemDefaults.ImplementationKeys
            .Where(key => !configuredKeys.Contains(key))
            .ToArray();

        if (missingKeys.Length > 0)
        {
            throw new InvalidOperationException(
                $"Gameplay item catalog is missing required rows: {string.Join(", ", missingKeys)}."
            );
        }

        return items;
    }

    private static GameplayItemDefinition Map(GameplayItemEntity entity)
    {
        var implementationKey = Required(entity.ImplementationKey, nameof(entity.ImplementationKey), 64);
        var defaults = GameplayItemDefaults.Get(implementationKey);
        var internalName = Required(entity.InternalName, nameof(entity.InternalName), 128);
        var displayName = Required(entity.DisplayName, nameof(entity.DisplayName), 128);
        var displayNameKey = LocalizationKeyValidator.Required(
            entity.DisplayNameKey,
            nameof(entity.DisplayNameKey)
        );
        var inheritorName = Required(entity.InheritorName, nameof(entity.InheritorName), 64);
        var model = Required(entity.Model, nameof(entity.Model), 512);

        if (!string.Equals(internalName, defaults.InternalName, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Gameplay item '{implementationKey}' must keep InternalName '{defaults.InternalName}'."
            );
        }

        if (!IsInheritorName(inheritorName))
        {
            throw new InvalidOperationException($"Gameplay item '{implementationKey}' has an invalid inheritor name.");
        }

        if (!IsModelPath(model))
        {
            throw new InvalidOperationException($"Gameplay item '{implementationKey}' has an invalid model path.");
        }

        var accessFlags = (AccessFlags)entity.AccessFlags;

        if ((accessFlags & ~AccessFlags.All) != 0)
        {
            throw new InvalidOperationException($"Gameplay item '{implementationKey}' has invalid access flags.");
        }

        if (!Enum.TryParse<ItemRarity>(entity.Rarity, true, out var rarity) || !Enum.IsDefined(rarity))
        {
            throw new InvalidOperationException($"Gameplay item '{implementationKey}' has an invalid rarity.");
        }

        var settings = ParseSettings(implementationKey, entity.SettingsJson);
        ValidateSettings(implementationKey, settings);

        return new GameplayItemDefinition(
            implementationKey,
            internalName,
            displayName,
            displayNameKey,
            inheritorName,
            accessFlags,
            rarity,
            model,
            entity.Enabled,
            entity.SortOrder,
            settings
        );
    }

    private static IGameplayItemBehaviorSettings ParseSettings(string implementationKey, string json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException($"Gameplay item '{implementationKey}' has empty settings.");
        }

        try
        {
            return implementationKey switch
            {
                GameplayItemKeys.BarrierNade => Deserialize<BarrierNadeSettings>(json),
                GameplayItemKeys.FireNade => Deserialize<FireNadeSettings>(json),
                GameplayItemKeys.FrostNade => Deserialize<FrostNadeSettings>(json),
                GameplayItemKeys.JumpNade => Deserialize<JumpNadeSettings>(json),
                GameplayItemKeys.ShakeNade => Deserialize<ShakeNadeSettings>(json),
                GameplayItemKeys.LaserMine => Deserialize<LaserMineSettings>(json),
                _ => throw new InvalidOperationException(
                    $"Unknown gameplay item implementation '{implementationKey}'."
                )
            };
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Gameplay item '{implementationKey}' has invalid settings JSON.",
                exception
            );
        }
    }

    private static TSettings Deserialize<TSettings>(string json)
        where TSettings : class, IGameplayItemBehaviorSettings
    {
        return JsonSerializer.Deserialize<TSettings>(json, JsonOptions)
               ?? throw new JsonException($"Could not deserialize {typeof(TSettings).Name}.");
    }

    private static void ValidateSettings(string key, IGameplayItemBehaviorSettings settings)
    {
        switch (settings)
        {
            case BarrierNadeSettings barrier:
                RequireParticlePath(key, barrier.Particle);
                RequireSoundEvent(key, barrier.KnockSound);
                RequireSoundEvent(key, barrier.EnvironmentSound);
                RequireRange(key, barrier.EnvironmentVolume, 0f, 10f);
                RequirePositive(key, barrier.Radius, barrier.Duration, barrier.TickInterval);
                RequireRange(key, barrier.HorizontalKnockback, 0f, 10_000f);
                RequireRange(key, barrier.GroundZBoost, 0f, 10_000f);
                RequireRange(key, barrier.AirZBoost, 0f, 10_000f);
                break;

            case FireNadeSettings fire:
                RequirePositive(key, fire.Radius, fire.Duration);
                RequireRange(key, fire.DamagePerTickPercent, 0f, 100f);
                RequireRange(key, fire.InstantDamagePercent, 0f, 100f);
                break;

            case FrostNadeSettings frost:
                RequirePositive(key, frost.Radius, frost.Duration);
                RequireRange(key, frost.DamageReduction, 0f, 1f);
                break;

            case JumpNadeSettings jump:
                RequirePositive(key, jump.Radius, jump.Power);
                break;

            case ShakeNadeSettings shake:
                RequirePositive(key, shake.Radius, shake.Duration);
                break;

            case LaserMineSettings mine:
                if (!IsModelPath(mine.MineModel))
                {
                    throw new InvalidOperationException($"Gameplay item '{key}' has an invalid mine model path.");
                }

                RequirePositive(
                    key,
                    mine.TriggerInterval,
                    mine.DamagePerTrigger,
                    mine.TracerDistance,
                    mine.BeamWidth,
                    mine.MaxDistanceToAttach,
                    mine.SetupDuration
                );

                if (mine.MaxHealth is < 1 or > 1_000_000 || mine.UpdateIntervalMs is < 16 or > 5_000)
                {
                    throw new InvalidOperationException($"Gameplay item '{key}' has out-of-range mine settings.");
                }
                break;

            default:
                throw new InvalidOperationException($"Gameplay item '{key}' has unsupported settings.");
        }
    }

    private static void RequirePositive(string key, params float[] values)
    {
        if (values.Any(value => !float.IsFinite(value) || value <= 0f || value > 1_000_000f))
        {
            throw new InvalidOperationException($"Gameplay item '{key}' has an invalid positive setting.");
        }
    }

    private static void RequireRange(string key, float value, float minimum, float maximum)
    {
        if (!float.IsFinite(value) || value < minimum || value > maximum)
        {
            throw new InvalidOperationException($"Gameplay item '{key}' has an out-of-range setting.");
        }
    }

    private static void RequireParticlePath(string key, string value)
    {
        if (!IsResourcePath(value, "particles/", ".vpcf"))
        {
            throw new InvalidOperationException($"Gameplay item '{key}' has an invalid particle path.");
        }
    }

    private static void RequireSoundEvent(string key, string value)
    {
        if (value.Length is < 2 or > 256 || !char.IsAsciiLetter(value[0]) ||
            value.Any(character =>
                !char.IsAsciiLetterOrDigit(character) && character is not '_' and not '.'
            ))
        {
            throw new InvalidOperationException($"Gameplay item '{key}' has an invalid sound event.");
        }
    }

    private static bool IsInheritorName(string value)
    {
        var normalized = value.StartsWith("weapon_", StringComparison.Ordinal)
            ? value[7..]
            : value;

        return normalized.Length > 0 && normalized.All(character =>
            character is >= 'a' and <= 'z' || char.IsAsciiDigit(character) || character == '_'
        );
    }

    private static bool IsModelPath(string value)
    {
        return (value.StartsWith("models/", StringComparison.Ordinal) ||
                value.StartsWith("weapons/", StringComparison.Ordinal)) &&
               IsResourcePath(value, string.Empty, ".vmdl");
    }

    private static bool IsResourcePath(string value, string prefix, string suffix)
    {
        return value.Length <= 512 &&
               !value.Contains("..", StringComparison.Ordinal) &&
               value.StartsWith(prefix, StringComparison.Ordinal) &&
               value.EndsWith(suffix, StringComparison.OrdinalIgnoreCase) &&
               value.All(character =>
                   char.IsAsciiLetterOrDigit(character) || character is '_' or '/' or '.' or ':' or '-'
               );
    }

    private static string Required(string? value, string field, int maximumLength)
    {
        var trimmed = value?.Trim();

        return string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > maximumLength
            ? throw new InvalidOperationException($"{field} is empty or too long.")
            : trimmed;
    }
}
