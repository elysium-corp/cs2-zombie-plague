using CustomEquipment.Data.GameplayItems;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class EquipmentLocalizationDefaultsTests
{
    [Theory]
    [InlineData(GameplayItemKeys.BarrierNade, "Equipment.Item.Custom.Equipment.Barrier.Nade.Name")]
    [InlineData(GameplayItemKeys.FireNade, "Equipment.Item.Custom.Equipment.Fire.Nade.Name")]
    [InlineData(GameplayItemKeys.FrostNade, "Equipment.Item.Custom.Equipment.Frost.Nade.Name")]
    [InlineData(GameplayItemKeys.JumpNade, "Equipment.Item.Custom.Equipment.Jump.Nade.Name")]
    [InlineData(GameplayItemKeys.ShakeNade, "Equipment.Item.Custom.Equipment.Shake.Nade.Name")]
    [InlineData(GameplayItemKeys.LaserMine, "Equipment.Item.Custom.Equipment.Laser.Mine.Name")]
    public void GameplayDefaults_ReuseExistingLocalizationKeys(
        string implementationKey,
        string expectedKey
    )
    {
        Assert.Equal(expectedKey, GameplayItemDefaults.Get(implementationKey).DisplayNameKey);
    }
}
