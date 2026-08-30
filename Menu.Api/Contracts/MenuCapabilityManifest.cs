using System.Text.Json.Serialization;

namespace Menu.Api.Contracts;

/// <summary>
/// Описывает возможности конкретного целевого игрового сервера.
/// </summary>
public sealed record MenuCapabilityManifest
{
    /// <summary>Manifest для старой реализации Menu.Core без Capability API.</summary>
    public static MenuCapabilityManifest Unsupported { get; } = new()
    {
        MenuCoreApiVersion = 0,
        SchemaVersion = 0
    };

    /// <summary>Технический ключ сервера, к которому относится manifest.</summary>
    [JsonPropertyName("serverKey")]
    public string? ServerKey { get; init; }

    /// <summary>Версия установленного Menu.Core.</summary>
    [JsonPropertyName("menuCoreVersion")]
    public string MenuCoreVersion { get; init; } = string.Empty;

    /// <summary>Версия публичного Menu API.</summary>
    [JsonPropertyName("menuCoreApiVersion")]
    public int MenuCoreApiVersion { get; init; } = MenuContractVersions.MenuCoreApiVersion;

    /// <summary>Версия нормализованной схемы.</summary>
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; init; } = MenuContractVersions.SchemaVersion;

    /// <summary>Версия установленного Swiftly Menu API.</summary>
    [JsonPropertyName("swiftlyMenuApiVersion")]
    public string SwiftlyMenuApiVersion { get; init; } = string.Empty;

    /// <summary>Поддерживаемые возможности по стабильным ключам.</summary>
    [JsonPropertyName("features")]
    public IReadOnlyDictionary<string, bool> Features { get; init; }
        = new Dictionary<string, bool>();

    /// <summary>Максимальная разрешённая глубина переходов на этом сервере.</summary>
    [JsonPropertyName("maximumNavigationDepth")]
    public int MaximumNavigationDepth { get; init; } = 16;

    /// <summary>Canonical lookup keys команд, зарезервированных runtime этого сервера.</summary>
    [JsonPropertyName("reservedCommands")]
    public IReadOnlyList<string> ReservedCommands { get; init; } = Array.Empty<string>();

    /// <summary>Момент построения manifest.</summary>
    [JsonPropertyName("observedAt")]
    public DateTimeOffset ObservedAt { get; init; }

    /// <summary>Проверяет объявленную поддержку возможности.</summary>
    /// <param name="featureKey">Стабильный ключ из <see cref="MenuFeatureKeys"/> или будущего расширения.</param>
    /// <returns><c>true</c>, если возможность известна и включена.</returns>
    public bool Supports(string featureKey)
    {
        return featureKey is not null
               && Features.TryGetValue(featureKey, out var supported)
               && supported;
    }
}
