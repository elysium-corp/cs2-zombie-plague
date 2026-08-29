using CustomEquipment.Policies;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class LaserMinePolicyTests
{
    [Theory]
    [InlineData(false, false, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [InlineData(true, true, false)]
    public void CanGrant_AllowsOnlyFirstMine(
        bool hasCarriedMine,
        bool grantInProgress,
        bool expected
    )
    {
        Assert.Equal(expected, LaserMinePolicy.CanGrant(hasCarriedMine, grantInProgress));
    }

    [Theory]
    [InlineData(false, false, true, false, false)]
    [InlineData(false, true, false, false, true)]
    [InlineData(true, false, false, false, false)]
    [InlineData(true, false, true, true, false)]
    [InlineData(true, false, true, false, true)]
    public void CanUseC4_AllowsOnlyAccessibleLaserMineOrPendingGrant(
        bool isLaserMine,
        bool grantInProgress,
        bool accessAllowed,
        bool hasOtherCarriedMine,
        bool expected
    )
    {
        Assert.Equal(
            expected,
            LaserMinePolicy.CanUseC4(
                isLaserMine,
                grantInProgress,
                accessAllowed,
                hasOtherCarriedMine
            )
        );
    }

    [Theory]
    [InlineData(false, true)]
    [InlineData(true, false)]
    public void ShouldRemoveC4_KeepsOnlyRegisteredLaserMine(bool isLaserMine, bool expected)
    {
        Assert.Equal(expected, LaserMinePolicy.ShouldRemoveC4(isLaserMine));
    }
}
