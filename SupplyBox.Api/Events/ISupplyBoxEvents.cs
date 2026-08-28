namespace SupplyBox.Api.Events;

using Common.Hooks.Abstractions;
using SupplyBox.Api.Events.Contexts;

/// <summary>
/// События модуля ящиков снабжения.
/// </summary>
public interface ISupplyBoxEvents
{
    /// <summary>Вызывается перед созданием ящика на карте; создание можно отменить.</summary>
    IHookSubscription<SupplyBoxSpawningContext> Spawning { get; }

    /// <summary>Вызывается после создания и регистрации ящика на карте.</summary>
    IHookSubscription<SupplyBoxSpawnedContext> Spawned { get; }

    /// <summary>Вызывается при ожидаемом отказе создания ящика.</summary>
    IHookSubscription<SupplyBoxSpawnRejectedContext> SpawnRejected { get; }

    /// <summary>Вызывается один раз после завершения спуска ящика.</summary>
    IHookSubscription<SupplyBoxLandedContext> Landed { get; }

    /// <summary>Вызывается перед выдачей содержимого ящика игроку; сбор можно отменить.</summary>
    IHookSubscription<SupplyBoxCollectingContext> Collecting { get; }

    /// <summary>Вызывается после успешной выдачи содержимого ящика игроку.</summary>
    IHookSubscription<SupplyBoxCollectedContext> Collected { get; }

    /// <summary>Вызывается при ожидаемом отказе сбора ящика.</summary>
    IHookSubscription<SupplyBoxCollectionRejectedContext> CollectionRejected { get; }

    /// <summary>Вызывается перед удалением сущностей ящика; удаление можно отменить.</summary>
    IHookSubscription<SupplyBoxDestroyingContext> Destroying { get; }

    /// <summary>Вызывается после удаления сущностей ящика и остановки его таймеров.</summary>
    IHookSubscription<SupplyBoxDestroyedContext> Destroyed { get; }
}
