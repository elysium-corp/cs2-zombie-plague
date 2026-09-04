using CustomKnife.Data.Knives;
using CustomKnife.Data.Models;
using CustomKnife.Database.Entities;
using Microsoft.EntityFrameworkCore;
using ZombiePlague.Api.Data;

namespace CustomKnife.Database;

internal sealed class KnifeCatalogRepository(
    IDbContextFactory<CustomKnifeDbContext> contextFactory
) : IKnifeCatalogRepository
{
    public IReadOnlyCollection<IKnife> GetEnabledKnives()
    {
        using var context = contextFactory.CreateDbContext();

        return context.Knives
            .AsNoTracking()
            .Where(knife => knife.Enabled)
            .OrderBy(knife => knife.SortOrder)
            .ThenBy(knife => knife.Id)
            .AsEnumerable()
            .Select(Map)
            .ToArray();
    }

    private static IKnife Map(KnifeEntity entity)
    {
        var internalName = Required(entity.InternalName, nameof(entity.InternalName), 64);
        var displayName = Required(entity.DisplayName, nameof(entity.DisplayName), 128);
        var displayNameKey = LocalizationKey(entity.DisplayNameKey, nameof(entity.DisplayNameKey));
        var model = Required(entity.Model, nameof(entity.Model), 512);
        var description = Required(entity.Description, nameof(entity.Description), 512);
        var descriptionKey = LocalizationKey(entity.DescriptionKey, nameof(entity.DescriptionKey));
        var requiredPermission = OptionalPermission(entity.RequiredPermission);

        if (!IsInternalName(internalName))
        {
            throw new InvalidOperationException($"Knife '{internalName}' has an invalid InternalName.");
        }

        if (!IsModelPath(model))
        {
            throw new InvalidOperationException($"Knife '{internalName}' has an invalid model path.");
        }

        if (!IsFiniteInRange(entity.Speed, 1f, 2_000f) ||
            !IsFiniteInRange(entity.KnockbackRecoil, 0f, 100_000f) ||
            !IsFiniteInRange(entity.KnockbackPickDistance, 0f, 100_000f) ||
            entity.Gravity is < 1 or > 10_000 ||
            !IsFiniteInRange(entity.DamageMultiplier, 0f, 1_000f))
        {
            throw new InvalidOperationException($"Knife '{internalName}' contains out-of-range gameplay values.");
        }

        return new KnifeDefinition(
            Enabled: entity.Enabled,
            InternalName: internalName,
            DisplayName: displayName,
            DisplayNameKey: displayNameKey,
            Model: model,
            Description: description,
            DescriptionKey: descriptionKey,
            Speed: entity.Speed,
            KnockbackData: new KnockbackData(
                entity.KnockbackRecoil,
                entity.KnockbackPickDistance
            ),
            Gravity: entity.Gravity,
            DamageMultiplier: entity.DamageMultiplier,
            RequiredPermission: requiredPermission
        );
    }

    private static string LocalizationKey(string? value, string field)
    {
        var key = Required(value, field, 191);

        if (key.Split('.').Any(segment =>
                segment.Length == 0 ||
                !(segment[0] is >= 'A' and <= 'Z' || char.IsAsciiDigit(segment[0])) ||
                segment.Skip(1).Any(character => !char.IsAsciiLetterOrDigit(character))))
        {
            throw new InvalidOperationException($"{field} is not a valid localization key.");
        }

        return key;
    }

    private static string? OptionalPermission(string? value)
    {
        var permission = value?.Trim();

        if (string.IsNullOrEmpty(permission))
        {
            return null;
        }

        if (permission.Length > 128 || !permission.All(character =>
                character is >= 'a' and <= 'z' ||
                char.IsAsciiDigit(character) ||
                character is '_' or '.' or ':' or '-'))
        {
            throw new InvalidOperationException($"Permission '{permission}' has an invalid format.");
        }

        return permission;
    }

    private static string Required(string? value, string field, int maximumLength)
    {
        var trimmed = value?.Trim();

        return string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > maximumLength
            ? throw new InvalidOperationException($"{field} is empty or too long.")
            : trimmed;
    }

    private static bool IsInternalName(string value)
    {
        return value.All(character =>
            character is >= 'a' and <= 'z' ||
            char.IsAsciiDigit(character) ||
            character is '_' or '.' or ':' or '-'
        );
    }

    private static bool IsModelPath(string value)
    {
        return !value.Contains("..", StringComparison.Ordinal) &&
               value.EndsWith(".vmdl", StringComparison.OrdinalIgnoreCase) &&
               value.All(character =>
                   char.IsAsciiLetterOrDigit(character) || character is '_' or '/' or '.' or ':' or '-'
               );
    }

    private static bool IsFiniteInRange(float value, float minimum, float maximum)
    {
        return float.IsFinite(value) && value >= minimum && value <= maximum;
    }
}
