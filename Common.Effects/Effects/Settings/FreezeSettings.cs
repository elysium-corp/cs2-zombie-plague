using Common.Effects.Effects.Contracts;

namespace Common.Effects.Effects.Settings;

public sealed class FreezeSettings(float duration = 5.0f, float damageReduction = 0.1f) : IEffectSettings
{
    public float Duration => duration;
    public float DamageReduction => damageReduction;
}