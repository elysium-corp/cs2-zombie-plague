using System.Text.Json;
using Xunit;
using ZombiePlague.Core.Catalog;
using ZombiePlague.Core.Config.Ability;

namespace ZombiePlague.Core.Tests;

public sealed class NewAbilityCatalogTests
{
    [Fact]
    public void DisarmParametersAreSupportedByCatalog()
    {
        var definition = Definition("disarm", new DisarmConfig());

        var config = Assert.IsType<DisarmConfig>(AbilityParameters.Parse(definition));

        Assert.Equal(600f, config.ProjectileSpeed);
        Assert.Equal(0.02f, config.UpdateIntervalSeconds);
    }

    [Fact]
    public void FireBombParametersAreSupportedByCatalog()
    {
        var definition = Definition("fire_bomb", new FireBombConfig());

        var config = Assert.IsType<FireBombConfig>(AbilityParameters.Parse(definition));

        Assert.Equal(215f, config.ExplosionRadius);
        Assert.Equal(3.1f, config.BurnDuration);
    }

    private static ZombieAbilityDefinition Definition(string kind, IAbilityConfig config)
    {
        var parameters = JsonSerializer.SerializeToElement(config, config.GetType(), ZombieCatalogDocument.JsonOptions);
        var dictionary = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(parameters, ZombieCatalogDocument.JsonOptions)!;
        dictionary.Remove(nameof(IAbilityConfig.Enable));

        return new ZombieAbilityDefinition
        {
            InternalName = kind,
            Kind = kind,
            Enabled = true,
            Side = "zombie",
            DisplayNameKey = $"ZombiePlague.Ability.{kind}.Name",
            DescriptionKey = $"ZombiePlague.Ability.{kind}.Description",
            Parameters = JsonSerializer.SerializeToElement(dictionary, ZombieCatalogDocument.JsonOptions),
        };
    }
}
