namespace ZombiePlague.Core.Store.Data;

internal readonly record struct AbilityHudPreferences(int ScalePercent, string Position)
{
    public const int DefaultScale = 100;
    public const string DefaultPosition = "bottom_center";
    public static readonly AbilityHudPreferences Default = new(DefaultScale, DefaultPosition);
    public static readonly int[] Scales = [50, 75, 100];
    public static readonly string[] Positions =
    [
        "top_left", "top_center", "top_right",
        "middle_left", "middle_center", "middle_right",
        "bottom_left", "bottom_center", "bottom_right"
    ];

    public AbilityHudPreferences Normalize() => new(
        Scales.Contains(ScalePercent) ? ScalePercent : DefaultScale,
        Positions.Contains(Position, StringComparer.Ordinal) ? Position : DefaultPosition);
}
