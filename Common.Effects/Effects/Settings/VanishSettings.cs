using Common.Effects.Effects.Contracts;

namespace Common.Effects.Effects.Settings;

public class VanishSettings(float duration = 5.0f) : IEffectSettings
{
    public float Duration => duration;
}