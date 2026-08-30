using System.Text.Json.Serialization;
using Menu.Api.Enums;

namespace Menu.Api.Results;

/// <summary>Описывает одну проблему валидации с точным путём к полю.</summary>
public sealed record MenuValidationIssue
{
    /// <summary>Важность проблемы.</summary>
    [JsonPropertyName("severity")]
    public MenuValidationSeverity Severity { get; init; } = MenuValidationSeverity.Error;

    /// <summary>Стабильный машинный код проблемы.</summary>
    [JsonPropertyName("code")]
    public string Code { get; init; } = string.Empty;

    /// <summary>JSON path или логический путь к некорректному полю.</summary>
    [JsonPropertyName("path")]
    public string? Path { get; init; }

    /// <summary>Диагностическое сообщение для журнала или CMS.</summary>
    [JsonPropertyName("message")]
    public string Message { get; init; } = string.Empty;
}

/// <summary>Представляет результат schema или provider-side validation.</summary>
public sealed record MenuValidationResult
{
    /// <summary>Общий успешный результат без проблем.</summary>
    public static MenuValidationResult Valid { get; } = new()
    {
        IsValid = true
    };

    /// <summary>Признак отсутствия блокирующих ошибок.</summary>
    [JsonPropertyName("isValid")]
    public bool IsValid { get; init; }

    /// <summary>Найденные ошибки, предупреждения и информационные сообщения.</summary>
    [JsonPropertyName("issues")]
    public IReadOnlyList<MenuValidationIssue> Issues { get; init; }
        = Array.Empty<MenuValidationIssue>();

    /// <summary>Создаёт неуспешный результат с одной проблемой.</summary>
    /// <param name="code">Стабильный машинный код.</param>
    /// <param name="message">Диагностическое сообщение.</param>
    /// <param name="path">Путь к полю или <c>null</c>.</param>
    /// <returns>Неуспешный результат provider-side validation.</returns>
    public static MenuValidationResult Invalid(string code, string message, string? path = null)
    {
        return new MenuValidationResult
        {
            IsValid = false,
            Issues =
            [
                new MenuValidationIssue
                {
                    Severity = MenuValidationSeverity.Error,
                    Code = code ?? string.Empty,
                    Message = message ?? string.Empty,
                    Path = path
                }
            ]
        };
    }
}

/// <summary>Представляет результат публичной runtime или registry операции.</summary>
public sealed record MenuOperationResult
{
    /// <summary>Общий успешный результат.</summary>
    public static MenuOperationResult Succeeded { get; } = new()
    {
        Status = MenuOperationStatus.Success
    };

    /// <summary>Итоговый статус.</summary>
    [JsonPropertyName("status")]
    public MenuOperationStatus Status { get; init; } = MenuOperationStatus.InvalidRequest;

    /// <summary>Стабильный машинный код результата.</summary>
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    /// <summary>Диагностическое сообщение, не предназначенное для прямого показа игроку.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>Подробности validation, если они доступны.</summary>
    [JsonPropertyName("issues")]
    public IReadOnlyList<MenuValidationIssue> Issues { get; init; }
        = Array.Empty<MenuValidationIssue>();

    /// <summary>Показывает, что операция успешно завершена.</summary>
    [JsonIgnore]
    public bool IsSuccess => Status == MenuOperationStatus.Success;

    /// <summary>Создаёт результат ожидаемой ошибки без генерации исключения.</summary>
    /// <param name="status">Неуспешный статус.</param>
    /// <param name="code">Стабильный машинный код.</param>
    /// <param name="message">Необязательное диагностическое сообщение.</param>
    /// <param name="issues">Необязательные подробности validation.</param>
    /// <returns>Новый результат операции.</returns>
    public static MenuOperationResult Failure(
        MenuOperationStatus status,
        string code,
        string? message = null,
        IReadOnlyList<MenuValidationIssue>? issues = null
    )
    {
        return new MenuOperationResult
        {
            Status = status,
            Code = code,
            Message = message,
            Issues = issues ?? Array.Empty<MenuValidationIssue>()
        };
    }

    /// <summary>Создаёт результат отсутствующей возможности старого Menu.Core.</summary>
    /// <param name="code">Стабильный машинный код.</param>
    /// <returns>Результат со статусом Unsupported.</returns>
    public static MenuOperationResult Unsupported(string code)
    {
        return Failure(MenuOperationStatus.Unsupported, code);
    }
}
