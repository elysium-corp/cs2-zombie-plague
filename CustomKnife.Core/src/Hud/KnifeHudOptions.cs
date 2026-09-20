namespace CustomKnife.Hud;

internal sealed class KnifeHudOptions
{
    public float RefreshIntervalSeconds { get; set; } = 0.25f;
    public int IdleTimeoutSeconds { get; set; } = 120;
    public Dictionary<string, KnifeHudAppearance> Knives { get; set; } = new(StringComparer.Ordinal);
}

internal sealed class KnifeHudAppearance
{
    // Image — ключ скомпилированного knife-hud-assets.json, а не HTTP URL.
    public string Image { get; set; } = "knife";
    public KnifeHudRarity Rarity { get; set; } = KnifeHudRarity.Common;
    public string? SubtitleKey { get; set; }
    public string[] BenefitKeys { get; set; } = [];
}

internal enum KnifeHudRarity { Common, Uncommon, Rare, Restricted, Classified, Elite, Prototype, Legendary }
