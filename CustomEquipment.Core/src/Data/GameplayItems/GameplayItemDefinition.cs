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
    public float ArmingDuration { get; init; } = 1f;
    public string InstallSound { get; init; } = "c4.plant";
    public string ChargeSound { get; init; } = "Weapon_Taser.Charging";
    public string ReadySound { get; init; } = "C4.PlantSoundB";
    public string DamageSound { get; init; } = "Weapon_Taser.Hit";
    public string DestroySound { get; init; } = "BaseGrenade.Explode";
    public float SoundVolume { get; init; } = 0.7f;
    public float DamageSoundInterval { get; init; } = 0.3f;
    public string SoundEventsResource { get; init; } = "soundevents/game_sounds_weapons.vsndevts";
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
