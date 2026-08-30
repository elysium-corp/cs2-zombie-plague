using SwiftlyS2.Shared.Players;

namespace Menu.Api.Contracts;

/// <summary>
/// Описывает runtime-запрос на открытие опубликованного меню.
/// </summary>
/// <remarks>
/// Menu.Core повторно проверяет право инициатора на массовое открытие, а затем
/// применяет ACL меню и пунктов отдельно к каждому выбранному получателю.
/// </remarks>
public sealed record MenuOpenRequest
{
    /// <summary>Игрок, инициировавший открытие.</summary>
    public IPlayer Caller { get; init; } = null!;

    /// <summary>Стабильный технический ключ меню в active Release.</summary>
    public string MenuKey { get; init; } = string.Empty;

    /// <summary>Необязательное безопасное переопределение аудитории из конфигурации.</summary>
    public MenuAudienceDefinition? AudienceOverride { get; init; }

    /// <summary>
    /// Явные получатели для <see cref="Enums.MenuAudienceKind.ExplicitTargets"/>
    /// или <c>null</c>, если они не заданы.
    /// </summary>
    public IReadOnlyCollection<IPlayer>? ExplicitTargets { get; init; }
}
