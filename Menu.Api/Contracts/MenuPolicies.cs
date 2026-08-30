using System.Text.Json.Serialization;
using Menu.Api.Enums;

namespace Menu.Api.Contracts;

/// <summary>Описывает область применения меню или команды.</summary>
public sealed record MenuScopeDefinition
{
    /// <summary>Тип области применения.</summary>
    [JsonPropertyName("kind")]
    public MenuScopeKind Kind { get; init; } = MenuScopeKind.Global;

    /// <summary>Ключ сервера для scope <see cref="MenuScopeKind.Server"/>.</summary>
    [JsonPropertyName("serverKey")]
    public string? ServerKey { get; init; }

    /// <summary>Ключ группы серверов для scope <see cref="MenuScopeKind.ServerGroup"/>.</summary>
    [JsonPropertyName("serverGroupKey")]
    public string? ServerGroupKey { get; init; }
}

/// <summary>Ссылается на меню по стабильным техническим ключам.</summary>
public sealed record MenuReferenceDefinition
{
    /// <summary>
    /// Необязательный ключ Provider. Значение <c>null</c> означает меню active Release.
    /// </summary>
    [JsonPropertyName("providerKey")]
    public string? ProviderKey { get; init; }

    /// <summary>Ключ меню active Release или экспортированного меню Provider.</summary>
    [JsonPropertyName("menuKey")]
    public string MenuKey { get; init; } = string.Empty;
}

/// <summary>Описывает проверку игровых permission через Admin.Core.</summary>
public sealed record MenuAccessPolicyDefinition
{
    /// <summary>Способ объединения permission.</summary>
    [JsonPropertyName("kind")]
    public MenuAccessPolicyKind Kind { get; init; } = MenuAccessPolicyKind.Public;

    /// <summary>Permission, участвующие в политике.</summary>
    [JsonPropertyName("permissions")]
    public IReadOnlyList<string> Permissions { get; init; } = Array.Empty<string>();
}

/// <summary>Описывает получателей меню независимо от права его вызвать.</summary>
public sealed record MenuAudienceDefinition
{
    /// <summary>Способ выбора получателей.</summary>
    [JsonPropertyName("kind")]
    public MenuAudienceKind Kind { get; init; } = MenuAudienceKind.Caller;

    /// <summary>
    /// Permission инициатора, необходимое для массовой аудитории; <c>null</c>
    /// означает применение глобального permission Menu.Core.
    /// </summary>
    [JsonPropertyName("invokePermission")]
    public string? InvokePermission { get; init; }

    /// <summary>
    /// SteamID64 сохранённых явных получателей. Runtime-вызов также может передать
    /// подключённых игроков через <see cref="MenuOpenRequest.ExplicitTargets"/>.
    /// </summary>
    [JsonPropertyName("explicitSteamIds")]
    public IReadOnlyList<ulong> ExplicitSteamIds { get; init; } = Array.Empty<ulong>();
}
