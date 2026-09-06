namespace ZombiePlague.Core.Experimental.AbilityHud;

internal sealed class AbilityHudConfig
{
    public bool Enabled { get; set; }
    public float RefreshSeconds { get; set; } = 0.25f;
    public int MaximumIcons { get; set; } = 8;
    public bool ShowNames { get; set; }
    public bool HideWhenMenuOpen { get; set; } = true;

    public void Validate()
    {
        if (!float.IsFinite(RefreshSeconds) || RefreshSeconds is < 0.1f or > 2)
            throw new InvalidDataException("AbilityHud.RefreshSeconds: допустимо от 0.1 до 2");
        if (MaximumIcons is < 1 or > AbilityHudFrame.SlotCount)
            throw new InvalidDataException("AbilityHud.MaximumIcons: допустимо от 1 до 12");
    }
}
