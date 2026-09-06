using System.Text.Json;
using ZombiePlague.Core.Config.Ability;
using static ZombiePlague.Core.Catalog.ZombieCatalogDocument;

namespace ZombiePlague.Core.Catalog;

internal static class AbilityParameters
{
    public static IAbilityConfig Parse(ZombieAbilityDefinition definition)
    {
        var type = definition.Kind switch
        {
            "heal" => typeof(HealConfig), "leap" => typeof(LeapConfig), "blind" => typeof(BlindConfig),
            "charge" => typeof(ChargeConfig), "trap" => typeof(TrapConfig), "catch" => typeof(CatchConfig),
            "double_jump" => typeof(DoubleJumpConfig),
            _ => throw new InvalidDataException($"Неизвестная механика способности {definition.Kind}")
        };
        Require(definition.Parameters.ValueKind == JsonValueKind.Object, "Параметры способности должны быть объектом");
        Require(!definition.Parameters.TryGetProperty("Enable", out _), "Включение задаётся полем Enabled способности");
        var config = (IAbilityConfig)(definition.Parameters.Deserialize(type, JsonOptions)
            ?? throw new InvalidDataException("Отсутствуют параметры способности"));
        config.Enable = definition.Enabled;
        foreach (var property in type.GetProperties().Where(item => item.Name != "Enable"))
        {
            var value = property.GetValue(config);
            Require(value is not null, $"Отсутствует параметр {property.Name}");
            if (value is bool) continue;
            if (value is string text) { ValidateString(property.Name, text); continue; }
            if (value is List<string> list)
            {
                Require(list.Count <= 32, "Слишком много эффектов способности");
                foreach (var item in list) ValidateString(property.Name, item);
                continue;
            }
            var (min, max) = Bounds(property.Name);
            Range(Convert.ToDouble(value), min, max, property.Name);
        }
        return config;
    }

    public static (double Minimum, double Maximum) Bounds(string name) => name switch
    {
        "SpeedUpdatePerTimeTick" => (0.01, 0.1),
        "ChargeTime" => (1, 120),
        "CooldownTime" => (0, 3600),
        "DurationParticleEffect" => (0, 60),
        "HealAmount" => (1, 1_000_000),
        "MaxSpeed" => (1, 2000),
        "BaseJumpUnits" => (1, 1000),
        "BeamWidth" => (0.01, 20),
        "LiveDuration" or "EffectDuration" => (0.1, 300),
        _ when name.Contains("Color") || name.StartsWith("Alpha") => (0, 255),
        _ when name.StartsWith("DurationEffect") || name.StartsWith("HoldTimeEffect") => (0, 60_000),
        _ => (0, 10_000)
    };

    private static void ValidateString(string name, string value)
    {
        Text(value, name.StartsWith("Particle") ? 512 : 128);
        if (name.StartsWith("Particle") && value.Length > 0) Resource(value, ".vpcf");
    }

    public static IEnumerable<string> Resources(ZombieAbilityDefinition definition)
    {
        var config = Parse(definition);
        return config.GetType().GetProperties().Where(property => property.Name.StartsWith("Particle"))
            .SelectMany(property => property.GetValue(config) switch
            {
                string value => new[] { value },
                List<string> values => values,
                _ => Enumerable.Empty<string>()
            });
    }
}
