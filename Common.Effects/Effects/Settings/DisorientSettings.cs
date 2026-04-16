using Common.Effects.Effects.Contracts;

namespace Common.Effects.Effects.Settings;

public sealed class DisorientSettings(float duration = 5.0f) : IEffectSettings
{
    public float Duration => duration;
}