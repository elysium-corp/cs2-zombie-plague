using CustomEquipment.Api.Data.Models;
using CustomEquipment.Controllers;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class WeaponSoundSelectorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void EveryMatchingVariantCanBeSelectedWithItsOwnVolume(int index)
    {
        WeaponSound[] variants =
        [
            new() { Trigger = "fire", EventName = "Test.Fire1", Volume = 0.25f },
            new() { Trigger = "FIRE", EventName = "Test.Fire2", Volume = 0.5f },
            new() { Trigger = "fire", EventName = "Test.Fire3", Volume = 0.75f }
        ];
        WeaponSound[] sounds =
        [
            new() { Trigger = "reload", EventName = "Test.Reload1" },
            variants[0], variants[1],
            new() { Trigger = "reload", EventName = "Test.Reload2" },
            variants[2]
        ];

        var chosen = WeaponSoundSelector.Select(sounds, "Fire", new FixedIndexRandom(index, 3));

        Assert.Same(variants[index], chosen);
        Assert.Equal(variants[index].Volume, chosen!.Volume);
    }

    [Fact]
    public void SingleVariantPreservesExistingBehavior()
    {
        var sound = new WeaponSound { Trigger = "reload", EventName = "Test.Reload", Volume = 0.4f };

        Assert.Same(sound, WeaponSoundSelector.Select([sound], "reload", new FixedIndexRandom(0, 1)));
    }

    [Fact]
    public void MissingTriggerAndEmptyPoolDoNotPlayOrDrawRandomNumbers()
    {
        WeaponSound[] sounds = [new() { Trigger = "fire", EventName = "Test.Fire" }];
        var random = new FixedIndexRandom(0, -1);

        Assert.Null(WeaponSoundSelector.Select(sounds, "reload", random));
        Assert.Null(WeaponSoundSelector.Select([], "fire", random));
    }

    [Fact]
    public void EachCallDrawsAgainAndMayRepeatThePreviousVariant()
    {
        WeaponSound[] sounds =
        [
            new() { Trigger = "fire", EventName = "Test.Fire1" },
            new() { Trigger = "fire", EventName = "Test.Fire2" }
        ];
        var random = new FixedIndexRandom(1, 2);

        Assert.Same(sounds[1], WeaponSoundSelector.Select(sounds, "fire", random));
        Assert.Same(sounds[1], WeaponSoundSelector.Select(sounds, "fire", random));
        Assert.Equal(2, random.Calls);
    }

    private sealed class FixedIndexRandom(int index, int expectedCount) : Random
    {
        internal int Calls { get; private set; }

        public override int Next(int maxValue)
        {
            Assert.Equal(expectedCount, maxValue);
            Calls++;
            return index;
        }
    }
}
