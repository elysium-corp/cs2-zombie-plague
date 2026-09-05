using Common.Hooks.Abstractions;
using SupplyBox.Data;
using SwiftlyS2.Shared.Players;

namespace SupplyBox.Api.Events.Contexts;

/// <summary>Причина ожидаемого отказа создания ящика.</summary>
public enum SupplyBoxSpawnRejectionReason
{
    /// <summary>Текущий режим раунда запрещает ящики.</summary>
    RoundNotSupported,

    /// <summary>Достигнут лимит одновременно активных ящиков.</summary>
    ActiveLimitReached,

    /// <summary>Случайная проверка шанса выпадения не пройдена.</summary>
    ChanceMissed,

    /// <summary>Создание отменено обработчиком.</summary>
    Cancelled,

    /// <summary>Для текущей карты не удалось выбрать уникальную точку.</summary>
    SpawnPointUnavailable
}

/// <summary>Причина ожидаемого отказа сбора ящика.</summary>
public enum SupplyBoxCollectionRejectionReason
{
    /// <summary>Сбор отменён обработчиком.</summary>
    Cancelled,

    /// <summary>Обработчик попытался заменить собираемый ящик.</summary>
    InvalidSupplyBox,

    /// <summary>Выбранный игрок не может собрать ящик.</summary>
    InvalidPlayer,

    /// <summary>Уничтожение сущности ящика отменено обработчиком.</summary>
    DestructionCancelled,

    /// <summary>Ни одну доступную награду выдать не удалось; ящик остаётся на карте.</summary>
    RewardUnavailable
}

/// <summary>Контекст ожидаемого отказа создания ящика.</summary>
public readonly struct SupplyBoxSpawnRejectedContext(SupplyBoxSpawnRejectionReason reason) : IPostHookContext
{
    /// <summary>Причина отказа.</summary>
    public SupplyBoxSpawnRejectionReason Reason { get; } = reason;
}

/// <summary>Контекст приземлившегося ящика.</summary>
public readonly struct SupplyBoxLandedContext(ISupplyBoxEntity supplyBox) : IPostHookContext
{
    /// <summary>Приземлившийся ящик.</summary>
    public ISupplyBoxEntity SupplyBox { get; } = supplyBox;
}

/// <summary>Контекст ожидаемого отказа сбора ящика.</summary>
public readonly struct SupplyBoxCollectionRejectedContext(
    IPlayer player,
    ISupplyBoxEntity supplyBox,
    SupplyBoxCollectionRejectionReason reason
) : IPostHookContext
{
    /// <summary>Игрок, пытавшийся собрать ящик.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Ящик, который пытались собрать.</summary>
    public ISupplyBoxEntity SupplyBox { get; } = supplyBox;

    /// <summary>Причина отказа.</summary>
    public SupplyBoxCollectionRejectionReason Reason { get; } = reason;
}

/// <summary>Контекст уничтожения сущностей ящика.</summary>
public struct SupplyBoxDestroyingContext(ISupplyBoxEntity supplyBox) : IPreHookContext
{
    /// <summary>Уничтожаемый ящик.</summary>
    public ISupplyBoxEntity SupplyBox { get; } = supplyBox;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel() => IsCancelled = true;
}

/// <summary>Контекст уничтоженных сущностей ящика.</summary>
public readonly struct SupplyBoxDestroyedContext(ISupplyBoxEntity supplyBox) : IPostHookContext
{
    /// <summary>Уничтоженный ящик.</summary>
    public ISupplyBoxEntity SupplyBox { get; } = supplyBox;
}
