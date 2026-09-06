using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using ZombiePlague.Core.Config.Zombie;
using ZombiePlague.Core.Config.Human;

namespace ZombiePlague.Core.Catalog;

internal sealed class ZombieCatalogDocument
{
    public const int MaximumBytes = 2 * 1024 * 1024;
    public static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        WriteIndented = true
    };

    public int FormatVersion { get; set; } = 2;
    public string DefaultClass { get; set; } = "";
    public string NemesisClass { get; set; } = "";
    public string DefaultHumanClass { get; set; } = "";
    public string SurvivorClass { get; set; } = "";
    public List<ZombieClassDefinition> Classes { get; set; } = [];
    public List<ZombieAbilityDefinition> Abilities { get; set; } = [];
    public List<PlayerAbilityAssignment> PlayerAbilities { get; set; } = [];

    public static ZombieCatalogDocument Parse(string json, HClassConfig? humans = null)
    {
        if (System.Text.Encoding.UTF8.GetByteCount(json) > MaximumBytes)
            throw new InvalidDataException("Каталог классов превышает 2 МБ");
        var document = JsonSerializer.Deserialize<ZombieCatalogDocument>(json, JsonOptions)
            ?? throw new InvalidDataException("Каталог классов пуст");
        CatalogUpgrade.Apply(document, humans ?? new HClassConfig());
        document.Validate();
        return document;
    }

    public void Validate()
    {
        Require(FormatVersion == 2, "Неизвестная версия формата каталога");
        Require(Classes is { Count: > 0 and <= 256 } && Abilities is { Count: <= 256 }, "Некорректный размер каталога");
        var abilities = new Dictionary<string, ZombieAbilityDefinition>(StringComparer.Ordinal);
        foreach (var ability in Abilities)
        {
            Require(ability is not null, "Пустая способность");
            Key(ability.InternalName);
            Require(abilities.TryAdd(ability.InternalName, ability), "Повтор ключа способности");
            Text(ability.DisplayName, 160); Text(ability.Description, 2000);
            LocalizationKey(ability.DisplayNameKey); LocalizationKey(ability.DescriptionKey);
            Require(ability.Side is "both" or "human" or "zombie", "Неизвестная сторона способности");
            _ = AbilityParameters.Parse(ability);
        }

        var classes = new Dictionary<string, ZombieClassDefinition>(StringComparer.Ordinal);
        foreach (var item in Classes)
        {
            Require(item is not null, "Пустой класс");
            Key(item.InternalName);
            Require(classes.TryAdd(item.InternalName, item), "Повтор ключа класса");
            Require(item.Kind is "zombie" or "nemesis" or "human" or "survivor", "Неизвестный тип класса");
            Text(item.DisplayName, 160); Text(item.Description, 2000);
            LocalizationKey(item.DisplayNameKey); LocalizationKey(item.DescriptionKey);
            Text(item.PreviewModel, 2048);
            if (item.Kind is "zombie" or "nemesis" || item.Model != "") Resource(item.Model, ".vmdl");
            Require(item.Armor is >= 0 and <= 1_000_000, "Броня вне диапазона");
            Require(item.Health is >= 1 and <= 1_000_000, "Здоровье вне диапазона");
            Range(item.Speed, 1, 2000, "Скорость");
            Range(item.Knockback, 0, 10, "Отдача");
            Require(item.Gravity is >= 1 and <= 4000, "Гравитация вне диапазона");
            Require(item.SortOrder is >= -10000 and <= 10000, "Порядок вне диапазона");
            Text(item.InfectionSound, 128);
            Require(item.HurtSounds is { Count: <= 32 }, "Некорректные звуки боли");
            foreach (var sound in item.HurtSounds) Text(sound, 128);
            Require(item.Abilities is { Count: <= 16 }, "Слишком много способностей класса");
            Require(item.Abilities.Distinct(StringComparer.Ordinal).Count() == item.Abilities.Count, "Повтор способности класса");
            foreach (var key in item.Abilities)
                Require(key is not null && abilities.ContainsKey(key), $"Способность {key} отсутствует");
        }
        Require(PlayerAbilities is { Count: <= 4096 }, "Слишком много персональных назначений");
        var players = new HashSet<string>(StringComparer.Ordinal);
        foreach (var assignment in PlayerAbilities)
        {
            Require(assignment is not null, "Пустое персональное назначение");
            Require(assignment.SteamId is { Length: 17 } && ulong.TryParse(assignment.SteamId, out var steamId) &&
                steamId is >= 76561197960265728 and <= 76561202255233023, "Укажите корректный SteamID64 игрока");
            Require(players.Add(assignment.SteamId), "Повтор назначения для SteamID64");
            Text(assignment.DisplayName, 160);
            Require(assignment.Abilities is { Count: <= 32 }, "Слишком много персональных способностей");
            Require(assignment.Abilities.Distinct(StringComparer.Ordinal).Count() == assignment.Abilities.Count, "Повтор персональной способности");
            foreach (var key in assignment.Abilities)
                Require(key is not null && abilities.ContainsKey(key), $"Персональная способность {key} отсутствует");
        }
        Require(classes.TryGetValue(DefaultClass, out var normal) && normal is { Enabled: true, Kind: "zombie" },
            "Нужен включённый обычный класс по умолчанию");
        Require(classes.TryGetValue(NemesisClass, out var boss) && boss is { Enabled: true, Kind: "nemesis" },
            "Нужен включённый класс Nemesis");
        Require(classes.TryGetValue(DefaultHumanClass, out var human) && human is { Enabled: true, Kind: "human" },
            "Нужен включённый обычный класс человека по умолчанию");
        Require(classes.TryGetValue(SurvivorClass, out var survivor) && survivor is { Enabled: true, Kind: "survivor" },
            "Нужен включённый класс Survivor");
    }

    public IEnumerable<string> Resources() => Classes.Select(item => item.Model)
        .Concat(Abilities.SelectMany(AbilityParameters.Resources)).Where(value => !string.IsNullOrWhiteSpace(value)).Distinct();

    internal static void Require([System.Diagnostics.CodeAnalysis.DoesNotReturnIf(false)] bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }

    internal static void LocalizationKey(string value) => Require(value is { Length: <= 160 } &&
        Regex.IsMatch(value, "^[A-Z][A-Za-z0-9]*(?:\\.[A-Z][A-Za-z0-9]*)+$"), "Нужен ключ локализации, например ZombiePlague.Class.Medic.Name");

    internal static void Key(string value) => Require(value is not null && Regex.IsMatch(value, "^[a-z][a-z0-9_]{0,63}$"), "Некорректный ключ каталога");
    internal static void Text(string value, int maximum, bool empty = true) =>
        Require(value is not null && value.Length <= maximum && (empty || !string.IsNullOrWhiteSpace(value)) && !value.Any(char.IsControl), "Некорректная строка каталога");
    internal static void Range(double value, double minimum, double maximum, string name) =>
        Require(double.IsFinite(value) && value >= minimum && value <= maximum, $"{name}: допустимо от {minimum} до {maximum}");
    internal static void Resource(string value, string extension)
    {
        Text(value, 512, false);
        Require(value.EndsWith(extension, StringComparison.OrdinalIgnoreCase) && !value.Contains("..") &&
            !value.StartsWith('/') && !value.Contains('\\') && !value.Contains(':'), "Некорректный путь ресурса");
    }
}

internal sealed class ZombieClassDefinition : IZClassConfig, IHClassConfig
{
    public string InternalName { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string Kind { get; set; } = "zombie";
    public string DisplayName { get; set; } = "";
    public string DisplayNameKey { get; set; } = "";
    public string Description { get; set; } = "";
    public string DescriptionKey { get; set; } = "";
    public string Model { get; set; } = "";
    public int Health { get; set; }
    public int Armor { get; set; }
    public float Speed { get; set; }
    public float Knockback { get; set; }
    public int Gravity { get; set; }
    public string InfectionSound { get; set; } = "";
    public List<string> HurtSounds { get; set; } = [];
    public List<string> Abilities { get; set; } = [];
    public int SortOrder { get; set; }
    public string PreviewModel { get; set; } = "";
}

internal sealed class ZombieAbilityDefinition
{
    public string InternalName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public string DisplayNameKey { get; set; } = "";
    public string DescriptionKey { get; set; } = "";
    public string Kind { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public string Side { get; set; } = "both";
    public JsonElement Parameters { get; set; }
}

internal sealed class PlayerAbilityAssignment
{
    // Строка сохраняет точность SteamID64 при обмене с JavaScript
    public string SteamId { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool Enabled { get; set; } = true;
    public List<string> Abilities { get; set; } = [];
}
