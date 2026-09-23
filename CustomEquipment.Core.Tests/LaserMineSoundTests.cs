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
    public void DamageSoundAllowsFirstHitAndLimitsRepeatedHitsToThreeHundredMilliseconds()
    {
        long now = 0;
        var emissions = new List<long>();
        var sounds = new LaserMineSoundPlayback(Settings, (_, _, _) => emissions.Add(now), () => now);
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
        var first = new LaserMineSoundPlayback(Settings, (name, _, _) => emissions.Add(name), () => 1000);
        var second = new LaserMineSoundPlayback(Settings, (name, _, _) => emissions.Add(name), () => 1000);
        first.Damage(default);
        first.Damage(default);
        second.Damage(default);
        first.Destroy(default);

        Assert.Equal(new[] { Settings.DamageSound, Settings.DamageSound, Settings.DestroySound }, emissions);
    }

    [Fact]
    public void CustomSoundKeepsConfiguredPositionAndVolume()
    {
        var settings = Settings with { DamageSound = "Elysium.MineZap", SoundVolume = 0.4f };
        var position = new Vector(10, 20, 30);
        var emissions = new List<(string Name, Vector Position, float Volume)>();
        var sounds = new LaserMineSoundPlayback(settings,
            (name, origin, volume) => emissions.Add((name, origin, volume)), () => 0);
        sounds.Damage(position);
        Assert.Equal((settings.DamageSound, position, 0.4f), Assert.Single(emissions));
    }

    [Theory]
    [InlineData("", 1f)]
    [InlineData("Elysium.MineZap", 0f)]
    public void DisabledSoundDoesNotEmit(string name, float volume)
    {
        var sounds = new LaserMineSoundPlayback(Settings with { DamageSound = name, SoundVolume = volume },
            (_, _, _) => Assert.Fail("Отключённый звук не должен воспроизводиться."), () => 0);
        sounds.Damage(default);
    }
}
