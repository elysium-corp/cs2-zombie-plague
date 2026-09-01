using CustomKnife.Data.Models;
using ZombiePlague.Api.Data;

namespace CustomKnife.Data.Knives;

internal sealed record KnifeDefinition(
    bool Enabled,
    string InternalName,
    string DisplayName,
    string Model,
    string Description,
    float Speed,
    KnockbackData KnockbackData,
    int Gravity,
    float DamageMultiplier,
    string? RequiredPermission
) : IKnife, IAccessControlledKnife;
