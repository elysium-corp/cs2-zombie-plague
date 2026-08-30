using System.Text.Json.Serialization;
using Menu.Api.Enums;

namespace Menu.Api.Contracts;

/// <summary>Описывает один включаемый alias, открывающий опубликованное меню.</summary>
public sealed record MenuCommandDefinition
{
    /// <summary>Стабильный ASCII-ключ записи команды.</summary>
    [JsonPropertyName("commandKey")]
    public string CommandKey { get; init; } = string.Empty;

    /// <summary>Пользовательский alias; chat-команды поддерживают полный UTF-8.</summary>
    [JsonPropertyName("alias")]
    public string Alias { get; init; } = string.Empty;

    /// <summary>Пространство имён alias.</summary>
    [JsonPropertyName("kind")]
    public MenuCommandKind Kind { get; init; } = MenuCommandKind.Chat;

    /// <summary>Признак регистрации alias в runtime.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; init; } = true;

    /// <summary>Ключ целевого меню active Release.</summary>
    [JsonPropertyName("menuKey")]
    public string MenuKey { get; init; } = string.Empty;

    /// <summary>Scope, участвующий в проверке конфликтов alias.</summary>
    [JsonPropertyName("scope")]
    public MenuScopeDefinition Scope { get; init; } = new();

    /// <summary>Режим подавления только для chat-команды.</summary>
    [JsonPropertyName("chatSuppression")]
    public ChatSuppressionMode ChatSuppression { get; init; } = ChatSuppressionMode.None;
}
