using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Mines;

/// <summary>Причина ожидаемого отказа размещения лазерной мины.</summary>
public enum MinePlacementRejectionReason
{
    /// <summary>Для игрока не удалось найти подходящую поверхность.</summary>
    InvalidSurface,

    /// <summary>Размещение отменено подписчиком.</summary>
    Cancelled,

    /// <summary>Подписчик указал недействительного игрока.</summary>
    InvalidPlayer
}

/// <summary>Контекст ожидаемого отказа размещения лазерной мины.</summary>
public readonly struct MinePlacementRejectedContext(
    IPlayer player,
    LaserMineEntityBase? mine,
    MinePlacementRejectionReason reason
) : IPostHookContext
{
    /// <summary>Игрок, пытавшийся разместить мину.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Подготовленная сущность мины, если она уже была создана.</summary>
    public LaserMineEntityBase? Mine { get; } = mine;

    /// <summary>Причина отказа.</summary>
    public MinePlacementRejectionReason Reason { get; } = reason;
}
