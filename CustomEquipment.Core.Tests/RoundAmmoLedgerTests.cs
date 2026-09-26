using CustomEquipment.Services;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class RoundAmmoLedgerTests
{
    [Fact]
    public void RoundRestartRefillIsRevertedToTheAmmoLeftAtRoundEnd()
    {
        var ledger = new RoundAmmoLedger();
        ledger.Record(10, 42, "weapon_ak47", new WeaponAmmo(7, 0, 12, 0));

        Assert.True(ledger.TryRestore(10, 42, "weapon_ak47", new WeaponAmmo(30, 0, 90, 90), out var restored));
        Assert.Equal(new WeaponAmmo(7, 0, 12, 0), restored);
    }

    [Fact]
    public void RestoringNeverAddsAmmo()
    {
        var ledger = new RoundAmmoLedger();
        ledger.Record(10, 42, "weapon_ak47", new WeaponAmmo(30, 0, 90, 0));

        Assert.True(ledger.TryRestore(10, 42, "weapon_ak47", new WeaponAmmo(12, 0, 120, 0), out var restored));
        Assert.Equal(new WeaponAmmo(12, 0, 90, 0), restored);

        Assert.False(ledger.TryRestore(10, 42, "weapon_ak47", new WeaponAmmo(5, 0, 40, 0), out var unchanged));
        Assert.Equal(new WeaponAmmo(5, 0, 40, 0), unchanged);
    }

    [Theory]
    [InlineData(11UL, 42U, "weapon_ak47")]
    [InlineData(10UL, 43U, "weapon_ak47")]
    [InlineData(10UL, 42U, "weapon_m4a1")]
    public void OnlyTheSameWeaponOfTheSameConnectionIsRestored(ulong session, uint weapon, string designerName)
    {
        var ledger = new RoundAmmoLedger();
        ledger.Record(10, 42, "weapon_ak47", new WeaponAmmo(1, 0, 0, 0));
        var current = new WeaponAmmo(30, 0, 90, 0);

        Assert.False(ledger.TryRestore(session, weapon, designerName, current, out var restored));
        Assert.Equal(current, restored);
    }

    [Fact]
    public void ClearingForgetsThePreviousRound()
    {
        var ledger = new RoundAmmoLedger();
        ledger.Record(10, 42, "weapon_ak47", new WeaponAmmo(1, 0, 0, 0));
        ledger.Clear();

        Assert.Equal(0, ledger.Count);
        Assert.False(ledger.TryRestore(10, 42, "weapon_ak47", new WeaponAmmo(30, 0, 90, 0), out _));
    }
}
