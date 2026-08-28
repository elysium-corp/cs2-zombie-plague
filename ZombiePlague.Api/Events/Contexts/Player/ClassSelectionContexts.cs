using Common.Hooks.Abstractions;
using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Api.Events.Contexts.Player;

/// <summary>Тип выбираемого класса игрока.</summary>
public enum PlayerClassKind
{
    /// <summary>Класс человека.</summary>
    Human,

    /// <summary>Класс зомби.</summary>
    Zombie
}

/// <summary>Причина ожидаемого отказа выбора класса.</summary>
public enum ClassSelectionRejectionReason
{
    /// <summary>Выбор отменён обработчиком.</summary>
    Cancelled,

    /// <summary>Обработчик указал пустой идентификатор класса.</summary>
    InvalidClassId,

    /// <summary>Для игрока ещё не создана persistent-сессия предпочтений.</summary>
    SessionUnavailable
}

/// <summary>Контекст выбора предпочтительного класса игрока.</summary>
public struct ClassSelectingContext(IPlayer player, string classId, PlayerClassKind kind) : IPreHookContext
{
    /// <summary>Игрок, меняющий предпочтение.</summary>
    public IPlayer Player { get; set; } = player;

    /// <summary>Изначально выбранный идентификатор класса.</summary>
    public string OriginalClassId { get; } = classId;

    /// <summary>Идентификатор класса. Может быть изменён обработчиком.</summary>
    public string ClassId { get; set; } = classId;

    /// <summary>Тип класса.</summary>
    public PlayerClassKind Kind { get; } = kind;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст сохранённого выбора класса.</summary>
public readonly struct ClassSelectedContext(IPlayer player, string classId, PlayerClassKind kind) : IPostHookContext
{
    /// <summary>Игрок, изменивший предпочтение.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Сохранённый идентификатор класса.</summary>
    public string ClassId { get; } = classId;

    /// <summary>Тип класса.</summary>
    public PlayerClassKind Kind { get; } = kind;
}

/// <summary>Контекст ожидаемого отказа выбора класса.</summary>
public readonly struct ClassSelectionRejectedContext(
    IPlayer player,
    string classId,
    PlayerClassKind kind,
    ClassSelectionRejectionReason reason
) : IPostHookContext
{
    /// <summary>Игрок, пытавшийся изменить предпочтение.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Запрошенный идентификатор класса.</summary>
    public string ClassId { get; } = classId;

    /// <summary>Тип класса.</summary>
    public PlayerClassKind Kind { get; } = kind;

    /// <summary>Причина отказа.</summary>
    public ClassSelectionRejectionReason Reason { get; } = reason;
}
