using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Items;

/// <summary>Причина ожидаемого отказа выдачи предмета.</summary>
public enum ItemGiveRejectionReason
{
    /// <summary>Игрок недействителен.</summary>
    InvalidPlayer,

    /// <summary>Предмет не зарегистрирован или недоступен игроку.</summary>
    CannotUse,

    /// <summary>Общая выдача отменена обработчиком.</summary>
    Cancelled,

    /// <summary>Обработчик подменил предмет или игрока недопустимым значением.</summary>
    InvalidReplacement,

    /// <summary>Выдача конкретного типа предмета отменена обработчиком.</summary>
    TypeSpecificCancelled,

    /// <summary>У игрока уже есть предмет, который разрешён только в одном экземпляре.</summary>
    AlreadyOwned,

    /// <summary>Выдача не изменила состояние игрока, например броня уже заполнена.</summary>
    NoEffect
}

/// <summary>Контекст ожидаемого отказа выдачи предмета.</summary>
public readonly struct ItemGiveRejectedContext(
    IPlayer player,
    string internalName,
    IItem? item,
    GiveAction action,
    ItemGiveRejectionReason reason
) : IPostHookContext
{
    /// <summary>Игрок, которому пытались выдать предмет.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Запрошенный внутренний идентификатор предмета.</summary>
    public string InternalName { get; } = internalName;

    /// <summary>Созданный экземпляр предмета, если он уже существовал.</summary>
    public IItem? Item { get; } = item;

    /// <summary>Способ выдачи.</summary>
    public GiveAction Action { get; } = action;

    /// <summary>Причина отказа.</summary>
    public ItemGiveRejectionReason Reason { get; } = reason;
}

/// <summary>Контекст технической ошибки при создании или постановке выдачи предмета.</summary>
public readonly struct ItemGiveFailedContext(
    IPlayer player,
    string internalName,
    GiveAction action,
    Exception exception
) : IPostHookContext
{
    /// <summary>Игрок, которому пытались выдать предмет.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Внутренний идентификатор предмета.</summary>
    public string InternalName { get; } = internalName;

    /// <summary>Способ выдачи.</summary>
    public GiveAction Action { get; } = action;

    /// <summary>Исключение, которое будет выброшено повторно после события.</summary>
    public Exception Exception { get; } = exception;
}
