using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;
using ZombiePlague.Core.Config.Core;
using ZombiePlague.Core.Data.Rounds.Contracts;

namespace ZombiePlague.Core.Tests;

public sealed class InfectionDamagePolicyTests
{
    [Fact]
    public void IsKnifeAttack_RejectsBlastDamage_EvenWhenKnifeIsActive()
    {
        var result = InfectionDamagePolicy.IsKnifeAttack(
            DamageTypes_t.DMG_BLAST,
            "weapon_knife_t"
        );

        Assert.False(result);
    }

    [Fact]
    public void IsKnifeAttack_AcceptsSlashDamage_FromKnife()
    {
        var result = InfectionDamagePolicy.IsKnifeAttack(
            DamageTypes_t.DMG_SLASH,
            "weapon_knife_t"
        );

        Assert.True(result);
    }

    [Fact]
    public void IsKnifeAttack_RejectsSlashDamage_FromAnotherWeapon()
    {
        var result = InfectionDamagePolicy.IsKnifeAttack(
            DamageTypes_t.DMG_SLASH,
            "weapon_hegrenade"
        );

        Assert.False(result);
    }

    [Fact]
    public void GetArmorDamage_UsesConfiguredPrimaryAndSecondaryValues()
    {
        var config = new ZombiePlagueCoreConfig
        {
            ZombiePrimaryAttackArmorDamage = 3,
            ZombieSecondaryAttackArmorDamage = 7
        };

        Assert.Equal(3, InfectionDamagePolicy.GetArmorDamage(CSWeaponMode.Primary_Mode, config));
        Assert.Equal(7, InfectionDamagePolicy.GetArmorDamage(CSWeaponMode.Secondary_Mode, config));
    }

    [Fact]
    public void GetArmorDamage_DefaultsToOneAndTwo()
    {
        var config = new ZombiePlagueCoreConfig();

        Assert.Equal(1, InfectionDamagePolicy.GetArmorDamage(CSWeaponMode.Primary_Mode, config));
        Assert.Equal(2, InfectionDamagePolicy.GetArmorDamage(CSWeaponMode.Secondary_Mode, config));
    }

    [Fact]
    public void GetArmorDamage_UsesSafeDefaultsForNonPositiveValues()
    {
        var config = new ZombiePlagueCoreConfig
        {
            ZombiePrimaryAttackArmorDamage = 0,
            ZombieSecondaryAttackArmorDamage = -2
        };

        Assert.Equal(1, InfectionDamagePolicy.GetArmorDamage(CSWeaponMode.Primary_Mode, config));
        Assert.Equal(2, InfectionDamagePolicy.GetArmorDamage(CSWeaponMode.Secondary_Mode, config));
    }

    [Theory]
    [InlineData(10, 1, 9)]
    [InlineData(1, 2, 0)]
    [InlineData(0, 2, 0)]
    [InlineData(10, -1, 10)]
    public void CalculateRemainingArmor_ClampsAtZero(
        int currentArmor,
        int armorDamage,
        int expectedArmor
    )
    {
        Assert.Equal(
            expectedArmor,
            InfectionDamagePolicy.CalculateRemainingArmor(currentArmor, armorDamage)
        );
    }

    [Theory]
    [InlineData(10, 2)]
    [InlineData(1, 10)]
    public void ResolveKnifeHit_AbsorbsHit_WhenArmorAndOtherHumansRemain(
        int currentArmor,
        int aliveHumanCount
    )
    {
        var outcome = InfectionDamagePolicy.ResolveKnifeHit(currentArmor, aliveHumanCount);

        Assert.Equal(InfectionKnifeHitOutcome.AbsorbWithArmor, outcome);
    }

    [Theory]
    [InlineData(0, 2)]
    [InlineData(-1, 10)]
    public void ResolveKnifeHit_Infects_WhenArmorIsEmptyAndOtherHumansRemain(
        int currentArmor,
        int aliveHumanCount
    )
    {
        var outcome = InfectionDamagePolicy.ResolveKnifeHit(currentArmor, aliveHumanCount);

        Assert.Equal(InfectionKnifeHitOutcome.Infect, outcome);
    }

    [Theory]
    [InlineData(100, 1)]
    [InlineData(0, 1)]
    [InlineData(100, 0)]
    public void ResolveKnifeHit_DamagesLastHuman_IgnoringCurrentArmor(
        int currentArmor,
        int aliveHumanCount
    )
    {
        var outcome = InfectionDamagePolicy.ResolveKnifeHit(currentArmor, aliveHumanCount);

        Assert.Equal(InfectionKnifeHitOutcome.DamageLastHuman, outcome);
    }
}
