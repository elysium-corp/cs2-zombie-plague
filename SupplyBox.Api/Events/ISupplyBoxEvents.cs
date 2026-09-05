namespace SupplyBox.Api.Events;

using Common.Hooks.Abstractions;
using SupplyBox.Api.Events.Contexts;

/// <summary>
/// События модуля ящиков снабжения.
/// </summary>
public interface ISupplyBoxEvents
{
    /// <summary>Вызывается перед созданием ящика на карте; создание можно отменить.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>`SupplyBox.TryDrop`, после проверок режима, шанса, лимита и выбора точки</description></item>
    /// <item><term>Частота</term><description>Раунд</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена пропускает текущую попытку создания</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<SupplyBoxSpawningContext> Spawning { get; }

    /// <summary>Вызывается после создания и регистрации ящика на карте.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После создания сущности и добавления в список активных ящиков</description></item>
    /// <item><term>Частота</term><description>Раунд</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: ящик ещё спускается</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<SupplyBoxSpawnedContext> Spawned { get; }

    /// <summary>Вызывается при ожидаемом отказе создания ящика.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>На всех ожидаемых ветках отказа: режим, лимит, шанс, отмена, отсутствие точки</description></item>
    /// <item><term>Частота</term><description>Раунд</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Низкий: полезно для диагностики конфигурации</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<SupplyBoxSpawnRejectedContext> SpawnRejected { get; }

    /// <summary>Вызывается один раз после завершения спуска ящика.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>Один раз в `SupplyBoxEntity.Think`, когда ящик достиг целевой высоты</description></item>
    /// <item><term>Частота</term><description>Раунд</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: callback выполняется из scheduler игрового потока</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<SupplyBoxLandedContext> Landed { get; }

    /// <summary>Вызывается перед выдачей содержимого ящика игроку; сбор можно отменить.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>При контакте допустимого игрока с ящиком, до удаления сущностей</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Средняя</description></item>
    /// <item><term>Риск</term><description>Высокий: проверка близости идёт каждые 0,2 с после приземления, но событие вызывается только для кандидата на сбор</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<SupplyBoxCollectingContext> Collecting { get; }

    /// <summary>Вызывается после успешной выдачи содержимого ящика игроку.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После удаления сущностей и остановки thinkers</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: выдача награды уже завершена; повторно выдавать её не следует</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<SupplyBoxCollectedContext> Collected { get; }

    /// <summary>Вызывается при ожидаемом отказе сбора ящика.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>При отмене, неверной подмене, недействительном игроке, отмене уничтожения или отсутствии доступной награды</description></item>
    /// <item><term>Частота</term><description>Редко</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Низкий: ящик остаётся доступным</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<SupplyBoxCollectionRejectedContext> CollectionRejected { get; }

    /// <summary>Вызывается перед удалением сущностей ящика; удаление можно отменить.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>Перед выдачей награды при подборе; очистка по времени жизни, концу раунда и выгрузке не отменяется</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Высокий: отмена прерывает сбор и сохраняет сущности</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<SupplyBoxDestroyingContext> Destroying { get; }

    /// <summary>Вызывается после удаления сущностей ящика и остановки его таймеров.</summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader><term>Характеристика</term><description>Значение</description></listheader>
    /// <item><term>Когда</term><description>После `Despawn` и отмены thinkers</description></item>
    /// <item><term>Частота</term><description>Игрок</description></item>
    /// <item><term>Нагрузка</term><description>Низкая</description></item>
    /// <item><term>Риск</term><description>Средний: `Collected` следует только при успешном подборе; очистка и истечение времени жизни также вызывают Destroyed</description></item>
    /// <item><term>Поток</term><description>Игровой поток</description></item>
    /// </list>
    /// </remarks>
    IHookSubscription<SupplyBoxDestroyedContext> Destroyed { get; }
}
