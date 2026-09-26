using System.Reflection;
using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Database;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class FrostNadeCatalogTests
{
    private const string LegacySettings = """
        {"radius":250,"duration":5,"damage_reduction":0.1}
        """;

    [Fact]
    public void ExistingCatalogGetsParticleDefaultsWithoutLosingGameplaySettings()
    {
        var settings = Parse(LegacySettings);
        Validate(settings);

        Assert.Equal(250f, settings.Radius);
        Assert.Equal(5f, settings.Duration);
        Assert.Equal(0.1f, settings.DamageReduction);
        Assert.Equal(
            "particles/zombieplague/icegrenade/icegrenade_trail.vpcf",
            settings.TrailParticle
        );
        Assert.Equal(
            "particles/zombieplague/icegrenade/icegrenade_explosion.vpcf",
            settings.ExplosionParticle
        );
        Assert.Equal(2.5f, settings.ExplosionParticleLifetime);
    }

    [Fact]
    public void CatalogAcceptsCustomParticleSettings()
    {
        const string json = """
            {"radius":250,"duration":5,"damage_reduction":0.1,
             "trail_particle":"particles/custom/frost_trail.vpcf",
             "explosion_particle":"particles/custom/frost_explosion.vpcf",
             "explosion_particle_lifetime":3.5}
            """;

        var settings = Parse(json);
        Validate(settings);

        Assert.Equal("particles/custom/frost_trail.vpcf", settings.TrailParticle);
        Assert.Equal("particles/custom/frost_explosion.vpcf", settings.ExplosionParticle);
        Assert.Equal(3.5f, settings.ExplosionParticleLifetime);
    }

    private static FrostNadeSettings Parse(string json) =>
        (FrostNadeSettings)typeof(GameplayItemCatalogRepository)
            .GetMethod("ParseSettings", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [GameplayItemKeys.FrostNade, json])!;

    private static void Validate(FrostNadeSettings settings) =>
        typeof(GameplayItemCatalogRepository)
            .GetMethod("ValidateSettings", BindingFlags.Static | BindingFlags.NonPublic)!
            .Invoke(null, [GameplayItemKeys.FrostNade, settings]);
}
