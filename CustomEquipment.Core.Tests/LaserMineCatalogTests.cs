using System.Reflection;
using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Database;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class LaserMineCatalogTests
{
    private const string LegacySettings = """
        {"mine_model":"models/lasermine.vmdl","trigger_interval":0.15,"damage_per_trigger":35,
         "tracer_distance":2000,"max_health":100,"beam_width":0.5,"beam_red":0,"beam_green":0,
         "beam_blue":255,"beam_alpha":255,"max_distance_to_attach":100,"setup_duration":1,
         "update_interval_ms":100}
        """;

    [Fact]
    public void ExistingCatalogGetsSoundDefaultsWithoutLosingGameplaySettings()
    {
        var settings = Parse(LegacySettings);
        Validate(settings);
        Assert.Equal(35f, settings.DamagePerTrigger);
        Assert.Equal(2f, settings.ArmingDuration);
        Assert.Equal(0.882358f, settings.InstallSoundDuration);
        Assert.Equal(1.109478f, settings.ChargeSoundDuration);
        Assert.Equal(1.287256f, settings.ReadySoundDuration);
        Assert.Equal(2f, settings.DestroySoundDuration);
        Assert.Equal(2f, settings.ReadySoundDelay);
        Assert.Equal(3.287256f, settings.ActivationDelay);
        Assert.Equal(0.3f, settings.DamageSoundInterval);
        Assert.Equal("ZombiePlague.lasermine_electric_zap", settings.DamageSound);
    }

    [Fact]
    public void CatalogAcceptsCustomAndDisabledSounds()
    {
        var json = LegacySettings[..LegacySettings.LastIndexOf('}')] + """
            ,"damage_sound":"Elysium.MineZap","ready_sound":"","sound_volume":0.4}
            """;
        var settings = Parse(json);
        Validate(settings);
        Assert.Equal("Elysium.MineZap", settings.DamageSound);
        Assert.Empty(settings.ReadySound);
        Assert.Equal(0.4f, settings.SoundVolume);
    }

    [Theory]
    [InlineData("interval")]
    [InlineData("duration")]
    [InlineData("volume")]
    [InlineData("install_duration")]
    [InlineData("charge_duration")]
    [InlineData("ready_duration")]
    [InlineData("destroy_duration")]
    [InlineData("null_sound")]
    public void InvalidSoundSettingsAreRejectedBeforeRuntime(string field)
    {
        var settings = Parse(LegacySettings);
        settings = field switch
        {
            "interval" => settings with { DamageSoundInterval = 0.1f },
            "duration" => settings with { ArmingDuration = float.NaN },
            "install_duration" => settings with { InstallSoundDuration = -1f },
            "charge_duration" => settings with { ChargeSoundDuration = float.PositiveInfinity },
            "ready_duration" => settings with { ReadySoundDuration = float.NaN },
            "destroy_duration" => settings with { DestroySoundDuration = -1f },
            "volume" => settings with { SoundVolume = 2f },
            _ => settings with { DamageSound = null! }
        };
        var exception = Assert.Throws<TargetInvocationException>(() => Validate(settings));
        Assert.IsType<InvalidOperationException>(exception.InnerException);
    }

    private static LaserMineSettings Parse(string json) =>
        (LaserMineSettings)typeof(GameplayItemCatalogRepository)
            .GetMethod("ParseSettings", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [GameplayItemKeys.LaserMine, json])!;

    private static void Validate(LaserMineSettings settings) =>
        typeof(GameplayItemCatalogRepository)
            .GetMethod("ValidateSettings", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [GameplayItemKeys.LaserMine, settings]);
}
