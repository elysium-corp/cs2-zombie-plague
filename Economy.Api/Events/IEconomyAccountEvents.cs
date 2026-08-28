using Common.Hooks.Abstractions;

namespace Economy.Api.Events;

/// <summary>События жизненного цикла денежных счетов.</summary>
public interface IEconomyAccountEvents
{
    /// <summary>Вызывается после создания runtime-сессии со стартовым балансом.</summary>
    IHookSubscription<EconomyAccountInitializedContext> Initialized { get; }

    /// <summary>Вызывается после объединения runtime-изменений с данными БД.</summary>
    IHookSubscription<EconomyAccountLoadedContext> Loaded { get; }

    /// <summary>Вызывается при технической ошибке загрузки счёта.</summary>
    IHookSubscription<EconomyAccountLoadFailedContext> LoadFailed { get; }

    /// <summary>Вызывается после удаления runtime-сессии непосредственно перед постановкой сохранения в очередь.</summary>
    IHookSubscription<EconomyAccountRemovedContext> Removed { get; }

    /// <summary>Вызывается после успешного сохранения dirty snapshot в БД.</summary>
    IHookSubscription<EconomyAccountSavedContext> Saved { get; }

    /// <summary>Вызывается при технической ошибке сохранения счёта.</summary>
    IHookSubscription<EconomyAccountSaveFailedContext> SaveFailed { get; }
}
