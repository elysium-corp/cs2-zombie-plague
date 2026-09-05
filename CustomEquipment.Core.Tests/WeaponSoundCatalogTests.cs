using CustomEquipment.Database;
using CustomEquipment.Database.Entities;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class WeaponSoundCatalogTests
{
    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.35f)]
    [InlineData(1.0f)]
    [InlineData(10.0f)]
    public void EventReference_LoadsWithoutFilesAndKeepsRuntimeVolume(float volume)
    {
        var entity = new WeaponSoundEntity
        {
            Trigger = " reload ",
            EventName = " ElysiumWeapons.PlasmaAK.Reload ",
            Volume = volume
        };

        var sound = WeaponCatalogRepository.MapSound(entity);

        Assert.Equal("reload", sound.Trigger);
        Assert.Equal("ElysiumWeapons.PlasmaAK.Reload", sound.EventName);
        Assert.Equal(volume, sound.Volume);
        Assert.Empty(sound.Files);
    }

    [Fact]
    public void LegacyExportMetadata_DoesNotPreventLoadingAnEventReference()
    {
        var sound = WeaponCatalogRepository.MapSound(new WeaponSoundEntity
        {
            Trigger = "fire",
            EventName = "ElysiumWeapons.PlasmaAK.Fire",
            ReplacesEventName = "Weapon_AK47.Single",
            Volume = 0.4f,
            SoundType = "",
            MixGroup = "",
            Pitch = 0,
            ExtraPropertiesJson = "invalid legacy JSON",
            Files = [new WeaponSoundFileEntity { FilePath = "legacy.wav", Track = 0 }]
        });

        Assert.Equal("Weapon_AK47.Single", sound.ReplacesEventName);
        Assert.Equal(0.4f, sound.Volume);
    }

    [Fact]
    public void ChoosingPreviouslyReplacedEvent_DoesNotSuppressItOrInvalidateTheCatalog()
    {
        var sound = WeaponCatalogRepository.MapSound(new WeaponSoundEntity
        {
            Trigger = "fire",
            EventName = "Weapon_AK47.Single",
            ReplacesEventName = "weapon_ak47.single",
            Volume = 1.0f
        });

        Assert.Equal("Weapon_AK47.Single", sound.EventName);
        Assert.Null(sound.ReplacesEventName);
    }

    [Theory]
    [InlineData(-0.01f)]
    [InlineData(10.01f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void InvalidRuntimeVolume_IsRejected(float volume)
    {
        var entity = new WeaponSoundEntity
        {
            Trigger = "fire",
            EventName = "ElysiumWeapons.PlasmaAK.Fire",
            Volume = volume
        };

        Assert.Throws<InvalidOperationException>(() => WeaponCatalogRepository.MapSound(entity));
    }
}
