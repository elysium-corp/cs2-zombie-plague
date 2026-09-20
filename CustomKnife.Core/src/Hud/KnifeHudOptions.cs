namespace CustomKnife.Hud;

internal sealed class KnifeHudOptions
{
    public float RefreshIntervalSeconds { get; set; } = 0.25f;
    public int IdleTimeoutSeconds { get; set; } = 120;
    public int DefaultScale { get; set; } = 100;
    public Dictionary<string, KnifeHudAppearance> Knives { get; set; } = new(StringComparer.Ordinal);
}

internal sealed class KnifeHudAppearance
{
    // Раздельные ключи VSVG и PNG в knife-hud-assets.json, не HTTP URL.
    public string? Icon { get; set; }
    public string? Preview { get; set; }
    // Совместимость прежнего конфига: Image используется только для PNG-превью.
    public string? Image { get; set; }
    public string? SubtitleKey { get; set; }
}
