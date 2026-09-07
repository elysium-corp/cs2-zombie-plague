namespace ZombiePlague.Core.Hud.AbilityHud;

internal sealed class AbilityHudConfig
{
    public bool Enabled { get; set; } = true;
    public float RefreshSeconds { get; set; } = 0.25f;
    public bool ShowNames { get; set; } = true;

    public void Validate()
    {
        if (!float.IsFinite(RefreshSeconds) || RefreshSeconds is < 0.1f or > 2)
            throw new InvalidDataException("AbilityHud.RefreshSeconds: допустимо от 0.1 до 2");
    }
}
