using System.Text.Json.Serialization;
using CustomEquipment.Api.Enums;

namespace CustomEquipment.Data.GameplayItems;

internal interface IGameplayItemBehaviorSettings
{
}

internal sealed record BarrierNadeSettings(
    string Particle,
    string KnockSound,
    string EnvironmentSound,
    float EnvironmentVolume,
    float Radius,
    float Duration,
    float TickInterval,
    float HorizontalKnockback,
    float GroundZBoost,
    float AirZBoost
) : IGameplayItemBehaviorSettings;

internal sealed record FireNadeSettings(
    float Radius,
    float Duration,
    float DamagePerTickPercent,
    float InstantDamagePercent
) : IGameplayItemBehaviorSettings;

internal sealed record FrostNadeSettings(
    float Radius,
    float Duration,
    float DamageReduction
) : IGameplayItemBehaviorSettings;

internal sealed record JumpNadeSettings(
    float Radius,
    float Power
) : IGameplayItemBehaviorSettings;

internal sealed record ShakeNadeSettings(
    float Radius,
    float Duration
) : IGameplayItemBehaviorSettings;

internal sealed record LaserMineSettings(
    string MineModel,
    float TriggerInterval,
    float DamagePerTrigger,
    float TracerDistance,
    int MaxHealth,
    float BeamWidth,
    byte BeamRed,
    byte BeamGreen,
    byte BeamBlue,
    byte BeamAlpha,
    float MaxDistanceToAttach,
    float SetupDuration,
    int UpdateIntervalMs
) : IGameplayItemBehaviorSettings
{
    public float ArmingDuration { get; init; } = 2f;
    public float InstallSoundDuration { get; init; } = 0.882358f;
    public float ChargeSoundDuration { get; init; } = 1.109478f;
    public float ReadySoundDuration { get; init; } = 1.287256f;
    [JsonIgnore]
    public float ReadySoundDelay => Math.Max(ArmingDuration, InstallSoundDuration + ChargeSoundDuration);
    [JsonIgnore]
    public float ActivationDelay => ReadySoundDelay + ReadySoundDuration;
    public string InstallSound { get; init; } = "ZombiePlague.lasermine_mechanism_click";
    public string ChargeSound { get; init; } = "ZombiePlague.lasermine_charge_up";
    public string ReadySound { get; init; } = "ZombiePlague.lasermine_ready";
    public string DamageSound { get; init; } = "ZombiePlague.lasermine_electric_zap";
    public string DestroySound { get; init; } = "ZombiePlague.lasermine_explosion";
    public float DestroySoundDuration { get; init; } = 2f;
    public float SoundVolume { get; init; } = 0.7f;
    public float DamageSoundInterval { get; init; } = 0.3f;
    // Поле хранится в БД после миграций звуков мины; без него строгий JSON не читает каталог.
    public string SoundEventsResource { get; init; } = "soundevents/game_sounds_elysium_weapons.vsndevts";
}

internal sealed record GameplayItemDefinition(
    string ImplementationKey,
    string InternalName,
    string DisplayName,
    string DisplayNameKey,
    string InheritorName,
    AccessFlags AccessFlags,
    ItemRarity Rarity,
    string Model,
    bool Enabled,
    int SortOrder,
    IGameplayItemBehaviorSettings Settings
);
