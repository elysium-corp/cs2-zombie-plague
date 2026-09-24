using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Services;
using SwiftlyS2.Shared.Natives;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class LaserMineSoundTests
{
    private static LaserMineSettings Settings =>
        (LaserMineSettings)GameplayItemDefaults.Get(GameplayItemKeys.LaserMine).Settings;

    [Fact]
    public void PlacedMinePlaysClickThenChargeThenReadyWithoutOverlappingStages()
    {
        var emitted = new List<(string Name, float Time)>();
        var callbacks = new List<(float Time, Action Callback)>();
        var now = 0f;
        var sounds = new LaserMineSoundPlayback(Settings, (name, _, _, _) => emitted.Add((name, now)), () => 0);
        Assert.Empty(emitted);
        sounds.Start(default, (time, callback) => callbacks.Add((time, callback)));
        Assert.Equal(("ZombiePlague.lasermine_mechanism_click", 0f), Assert.Single(emitted));

        foreach (var item in callbacks.OrderBy(item => item.Time))
        {
            now = item.Time;
            item.Callback();
        }

        Assert.Equal(new[]
        {
            ("ZombiePlague.lasermine_mechanism_click", 0f),
            ("ZombiePlague.lasermine_charge_up", 0.882358f),
            ("ZombiePlague.lasermine_ready", 2f)
        }, emitted);
        Assert.True(emitted[2].Time >= emitted[1].Time + Settings.ChargeSoundDuration);
        Assert.Equal(emitted[2].Time + Settings.ReadySoundDuration, Settings.ActivationDelay);
    }

    [Fact]
    public void LongCustomSoundsDelayReadinessAndActivation()
    {
        var settings = Settings with { InstallSoundDuration = 2f, ChargeSoundDuration = 3f, ReadySoundDuration = 1f };
        var callbacks = new List<float>();
        new LaserMineSoundPlayback(settings, (_, _, _, _) => { }, () => 0)
            .Start(default, (time, _) => callbacks.Add(time));
        Assert.Equal(new[] { 2f, 5f }, callbacks);
        Assert.Equal(6f, settings.ActivationDelay);
    }

    [Fact]
    public void DamageSoundAllowsFirstHitAndLimitsRepeatedHitsToThreeHundredMilliseconds()
    {
        long now = 0;
        var emissions = new List<long>();
        var sounds = new LaserMineSoundPlayback(Settings, (_, _, _, _) => emissions.Add(now), () => now);
        foreach (var time in new long[] { 0, 100, 150, 299, 300, 450, 599, 600 })
        {
            now = time;
            sounds.Damage(default);
        }

        Assert.Equal(new long[] { 0, 300, 600 }, emissions);
    }

    [Fact]
    public void DamageCooldownIsIndependentForEachMineAndDoesNotSuppressExplosion()
    {
        var emissions = new List<string>();
        var first = new LaserMineSoundPlayback(Settings, (name, _, _, _) => emissions.Add(name), () => 1000);
        var second = new LaserMineSoundPlayback(Settings, (name, _, _, _) => emissions.Add(name), () => 1000);
        first.Damage(default);
        first.Damage(default);
        second.Damage(default);
        first.Destroy(default);

        Assert.Equal(new[] { Settings.DamageSound, Settings.DamageSound, Settings.DestroySound }, emissions);
    }

    [Fact]
    public void ExplosionUsesPositionEmitterInsteadOfEntityBeingRemoved()
    {
        var live = new List<string>();
        var destroyed = new List<string>();
        var sounds = new LaserMineSoundPlayback(Settings,
            (name, _, _, _) => live.Add(name), () => 0,
            (name, _, _, _) => destroyed.Add(name));
        sounds.Damage(default);
        sounds.Destroy(default);
        Assert.Equal(Settings.DamageSound, Assert.Single(live));
        Assert.Equal(Settings.DestroySound, Assert.Single(destroyed));
    }

    [Fact]
    public void CustomSoundKeepsConfiguredPositionAndVolume()
    {
        var settings = Settings with { DamageSound = "Elysium.MineZap", SoundVolume = 0.4f };
        var position = new Vector(10, 20, 30);
        var emissions = new List<(string Name, Vector Position, float Volume)>();
        var sounds = new LaserMineSoundPlayback(settings,
            (name, origin, volume, _) => emissions.Add((name, origin, volume)), () => 0);
        sounds.Damage(position);
        Assert.Equal((settings.DamageSound, position, 0.4f), Assert.Single(emissions));
    }

    [Theory]
    [InlineData("", 1f)]
    [InlineData("Elysium.MineZap", 0f)]
    public void DisabledSoundDoesNotEmit(string name, float volume)
    {
        var sounds = new LaserMineSoundPlayback(Settings with { DamageSound = name, SoundVolume = volume },
            (_, _, _, _) => Assert.Fail("Отключённый звук не должен воспроизводиться."), () => 0);
        sounds.Damage(default);
    }
}
