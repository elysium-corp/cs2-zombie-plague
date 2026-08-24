namespace CustomEquipment.Api.Data.Models;

public sealed class WeaponSound
{
    public required string Trigger { get; init; }

    public required string EventName { get; init; }

    public string? ReplacesEventName { get; init; }

    public string Type { get; init; } = "csgo_mega";

    public float Volume { get; init; } = 1.0f;

    public float Pitch { get; init; } = 1.0f;

    public string MixGroup { get; init; } = "Weapons";

    public bool PreloadVsnds { get; init; } = true;

    public string? ExtraPropertiesJson { get; init; }

    public IReadOnlyCollection<WeaponSoundFile> Files { get; init; } = Array.Empty<WeaponSoundFile>();
}

public sealed class WeaponSoundFile
{
    public int Track { get; init; } = 1;

    public required string Path { get; init; }

    public int SortOrder { get; init; }
}

public static class WeaponSoundTriggers
{
    public const string Fire = "fire";
    public const string Reload = "reload";
    public const string Empty = "empty";
    public const string Draw = "draw";
    public const string Inspect = "inspect";
    public const string Zoom = "zoom";
    public const string SilencerOn = "silencer_on";
    public const string SilencerOff = "silencer_off";
}
