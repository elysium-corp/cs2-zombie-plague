using CustomEquipment.Api.Data.Models;
using CustomEquipment.Database;
using CustomEquipment.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class WeaponHandlingCatalogTests
{
    [Fact]
    public void ExistingCatalogAndInstancesKeepDefaultHandling()
    {
        var weapon = WeaponCatalogRepository.Map(Weapon());

        Assert.Null(weapon.WeaponRecoil);
        Assert.Null(weapon.WeaponAccuracy);
        Assert.Null(weapon.CreateInstance().WeaponRecoil);
        Assert.Null(weapon.CreateInstance().WeaponAccuracy);
    }

    [Fact]
    public void HandlingSettingsReachIssuedInstancesWithoutFillingOmittedFields()
    {
        var entity = Weapon();
        entity.RecoilJson = """{"magnitude":[15,10],"magnitude_variance":[0,0],"angle":[-20,20]}""";
        entity.AccuracyJson = """{"spread":[0.001,0],"inaccuracy_fire":[0],"inaccuracy_jump_apex":0}""";

        var weapon = WeaponCatalogRepository.Map(entity).CreateInstance();

        Assert.Equal([15f, 10f], weapon.WeaponRecoil!.Magnitude);
        Assert.Equal([-20f, 20f], weapon.WeaponRecoil.Angle);
        Assert.Equal([0f, 0f], weapon.WeaponRecoil.MagnitudeVariance);
        Assert.Empty(weapon.WeaponRecoil.AngleVariance);
        Assert.Equal([0.001f, 0f], weapon.WeaponAccuracy!.Spread);
        Assert.Equal([0f], weapon.WeaponAccuracy.InaccuracyFire);
        Assert.Equal(0f, weapon.WeaponAccuracy.InaccuracyJumpApex);
        Assert.Null(weapon.WeaponAccuracy.InaccuracyJumpInitial);
        Assert.Empty(weapon.WeaponAccuracy.InaccuracyMove);
    }

    [Theory]
    [InlineData("{\"magnitude\":[1,2,3]}", true)]
    [InlineData("{\"magnitude\":[-1]}", true)]
    [InlineData("{\"angle\":[181]}", true)]
    [InlineData("{\"angle_variance\":[-1]}", true)]
    [InlineData("{\"magnitude\":null}", true)]
    [InlineData("{\"spread\":[1,2,3]}", false)]
    [InlineData("{\"spread\":[-1]}", false)]
    [InlineData("{\"spread\":[1e100]}", false)]
    [InlineData("{\"inaccuracy_jump_initial\":-1}", false)]
    [InlineData("{\"inaccuracy_move\":null}", false)]
    [InlineData("{\"unknown_field\":1}", false)]
    [InlineData("[]", false)]
    [InlineData("null", false)]
    [InlineData("invalid JSON", false)]
    public void InvalidCatalogSettingsAreRejectedBeforeNativeAccess(string json, bool recoil)
    {
        var entity = Weapon();
        if (recoil) entity.RecoilJson = json;
        else entity.AccuracyJson = json;

        Assert.Throws<InvalidOperationException>(() => WeaponCatalogRepository.Map(entity));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(float.NegativeInfinity)]
    public void CompiledWeaponsCannotWriteNonFiniteValues(float value)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponRecoil { Magnitude = [value] }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponAccuracy { Spread = [value] }.Validate());
        Assert.Throws<ArgumentOutOfRangeException>(() => new WeaponAccuracy { InaccuracyReload = value }.Validate());
    }

    [Fact]
    public void MigrationAddsOptionalSettingsAndModelMatchesSnapshot()
    {
        var options = new DbContextOptionsBuilder<CustomEquipmentDbContext>()
            .UseNpgsql("Host=localhost;Database=handling_test;Username=test;Password=test")
            .Options;
        using var context = new CustomEquipmentDbContext(options);
        var migrator = context.GetService<IMigrator>();
        var script = migrator.GenerateScript("20260909120000_AddWeaponHudIcon", "20260921120000_AddWeaponHandling");

        Assert.Contains("ADD recoil jsonb", script);
        Assert.Contains("ADD accuracy jsonb", script);
        Assert.Contains("CK_weapons_recoil_object", script);
        Assert.Contains("CK_weapons_accuracy_object", script);
        Assert.DoesNotContain("NOT NULL", script);
        Assert.DoesNotContain("UPDATE custom_equipment.weapons", script);
        Assert.False(context.Database.HasPendingModelChanges());

        var rollback = migrator.GenerateScript("20260921120000_AddWeaponHandling", "20260909120000_AddWeaponHudIcon");
        Assert.Contains("DROP COLUMN recoil", rollback);
        Assert.Contains("DROP COLUMN accuracy", rollback);
    }

    private static WeaponEntity Weapon() => new()
    {
        InternalName = "test_rifle",
        DisplayName = "Test rifle",
        DisplayNameKey = "Equipment.TestRifle.Name",
        InheritorName = "ak47",
        SubclassName = "test_rifle",
        Slot = "Primary",
        WeaponType = "Rifle",
        Rarity = "Common",
        AccessFlags = 1,
        Model = "models/test_rifle.vmdl"
    };
}
