using Common.Effects.Effects.Contracts;

namespace Common.Effects.Effects.Settings;

public sealed class FreezeSettings(float duration = 5.0f) : IEffectSettings
{
    public float Duration => duration;
}