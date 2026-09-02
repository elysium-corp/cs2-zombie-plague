using CustomKnife.Data.Models;
using ZombiePlague.Api.Data;

namespace CustomKnife.Data.Knives;

internal sealed record KnifeDefinition(
    bool Enabled,
    string InternalName,
    string DisplayName,
    string DisplayNameKey,
    string Model,
    string Description,
    string DescriptionKey,
    float Speed,
    KnockbackData KnockbackData,
    int Gravity,
    float DamageMultiplier,
    string? RequiredPermission
) : IKnife, IAccessControlledKnife, ILocalizedKnife;
