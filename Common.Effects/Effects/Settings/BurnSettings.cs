using Common.Effects.Effects.Contracts;

namespace Common.Effects.Effects.Settings;

public sealed class BurnSettings(
    float duration = 1.0f,
    float damagePerTickInPercent = 1.0f,
    float instantDamageInPercent = 1.0f)
    : IEffectSettings
{
    public float Duration => duration;
    public float DamagePerTickInPercent => damagePerTickInPercent;
    public float InstantDamageInPercent => instantDamageInPercent;
}