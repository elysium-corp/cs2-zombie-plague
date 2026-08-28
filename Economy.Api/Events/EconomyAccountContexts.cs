using Common.Hooks.Abstractions;
using SwiftlyS2.Shared.Players;

namespace Economy.Api.Events;

/// <summary>Контекст созданной runtime-сессии денежного счёта.</summary>
public readonly struct EconomyAccountInitializedContext(IPlayer player, int balance) : IPostHookContext
{
    /// <summary>Игрок, для которого создана сессия.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Стартовый баланс до завершения загрузки.</summary>
    public int Balance { get; } = balance;
}

/// <summary>Контекст завершённой загрузки денежного счёта.</summary>
public readonly struct EconomyAccountLoadedContext(ulong steamId, int balance, bool isNew) : IPostHookContext
{
    /// <summary>SteamID64 владельца счёта.</summary>
    public ulong SteamId { get; } = steamId;

    /// <summary>Объединённый runtime-баланс.</summary>
    public int Balance { get; } = balance;

    /// <summary>Показывает, что записи в БД ещё не существовало.</summary>
    public bool IsNew { get; } = isNew;
}

/// <summary>Контекст ошибки загрузки денежного счёта.</summary>
public readonly struct EconomyAccountLoadFailedContext(ulong steamId, Exception exception) : IPostHookContext
{
    /// <summary>SteamID64 владельца счёта.</summary>
    public ulong SteamId { get; } = steamId;

    /// <summary>Исключение, которое будет передано инфраструктуре фоновых задач.</summary>
    public Exception Exception { get; } = exception;
}

/// <summary>Контекст удаления runtime-сессии денежного счёта.</summary>
public readonly struct EconomyAccountRemovedContext(ulong steamId, int balance) : IPostHookContext
{
    /// <summary>SteamID64 владельца счёта.</summary>
    public ulong SteamId { get; } = steamId;

    /// <summary>Последний runtime-баланс, поставленный на сохранение.</summary>
    public int Balance { get; } = balance;
}

/// <summary>Контекст успешно сохранённого денежного счёта.</summary>
public readonly struct EconomyAccountSavedContext(ulong steamId, int balance) : IPostHookContext
{
    /// <summary>SteamID64 владельца счёта.</summary>
    public ulong SteamId { get; } = steamId;

    /// <summary>Сохранённый баланс.</summary>
    public int Balance { get; } = balance;
}

/// <summary>Контекст ошибки сохранения денежного счёта.</summary>
public readonly struct EconomyAccountSaveFailedContext(ulong steamId, Exception exception) : IPostHookContext
{
    /// <summary>SteamID64 владельца счёта.</summary>
    public ulong SteamId { get; } = steamId;

    /// <summary>Исключение, которое будет передано инфраструктуре фоновых задач.</summary>
    public Exception Exception { get; } = exception;
}
