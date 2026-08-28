# Каталог событий

Документ описывает публичные события `Common.Hooks`, добавленные в API модулей. Все перечисленные события подключены к указанным точкам `Core`: наличие события в таблице означает, что оно действительно отправляется, а не только объявлено в контракте.

> Файл генерируется из XML-комментариев свойств `IHookSubscription<T>`. Изменяйте документацию рядом с контрактом события и запускайте генератор; ручные правки таблиц будут отклонены CI.

## Модель выполнения

- Обработчики выполняются **синхронно в потоке вызывающего кода**. Событие не переносит работу в фон и не меняет поток.
- `...ing` и `...Processing` — контексты до операции. Они реализуют `IPreHookContext`, могут менять явно изменяемые свойства и вызывать `Cancel()`.
- `...ed`, `...Committed`, `...Rejected` и `...Failed` — уведомления о результате. Их контексты неизменяемы.
- `...Rejected` означает ожидаемый отказ без технической ошибки. `...Failed` передаёт исключение; исходный код после отправки события повторно выбрасывает его.
- Отмена остаётся установленной, но не прерывает цепочку: оставшиеся обработчики также вызываются. Это позволяет аудиторам и низкоприоритетным обработчикам увидеть отменённую операцию.
- Исключение одного обработчика изолируется `HookService` и не останавливает остальных. Модуль может передать обработчик ошибок в `HookService`; без него исключение подписчика подавляется.
- Список обработчиков хранится по схеме copy-on-write. `Dispatch` не создаёт snapshot-массив на каждом игровом событии; выделение памяти происходит при подписке и отписке.

Подписка и обязательная симметричная отписка:

```csharp
api.Events.Players.Infected.Hook(OnPlayerInfected, HookPriority.Normal);

// При выгрузке плагина:
api.Events.Players.Infected.Unhook(OnPlayerInfected);
```

## Переход со старых имён

Старые `Events.Pre`/`Events.Post` и пары `X`/`XEvent` удалены. Одна операция теперь представлена двумя однозначными стадиями:

| Старый контракт | Новый контракт |
|---|---|
| `ZombiePlague.Events.Pre.PlayerInfect` / `PlayerInfectEvent` | `ZombiePlague.Events.Players.Infecting` |
| `ZombiePlague.Events.Post.PlayerInfect` / `PlayerInfectEvent` | `ZombiePlague.Events.Players.Infected` |
| `ZombiePlague.Events.Pre.RoundStart` | `ZombiePlague.Events.Rounds.Starting` |
| `ZombiePlague.Events.Post.RoundStart` | `ZombiePlague.Events.Rounds.Started` |
| `CustomEquipment.Events.Pre.ItemBuy` | `CustomEquipment.Events.Items.Purchasing` |
| `CustomEquipment.Events.Post.ItemBuy` | `CustomEquipment.Events.Items.Purchased` |
| `CustomEquipment.Events.Pre/Post.ItemGive` | `CustomEquipment.Events.Items.Giving/Given` |
| `CustomEquipment.Events.Pre/Post.WeaponGive` | `CustomEquipment.Events.Weapons.Giving/Given` |
| `CustomEquipment.Events.Pre/Post.GrenadeGive` | `CustomEquipment.Events.Grenades.Giving/Given` |
| `CustomEquipment.Events.Pre/Post.GrenadeThrow` | `CustomEquipment.Events.Grenades.Throwing/Thrown` |
| `CustomEquipment.Events.Pre/Post.GrenadeDetonate` | `CustomEquipment.Events.Grenades.Detonating/Detonated` |
| `CustomEquipment.Events.Pre/Post.MinePlace` | `CustomEquipment.Events.Mines.Placing/Placed` |
| `SupplyBox.Events.Pre/Post.Drop` | `SupplyBox.Events.Spawning/Spawned` |
| `SupplyBox.Events.Pre/Post.PickUp` | `SupplyBox.Events.Collecting/Collected` |

## Оценка нагрузки и риска

Частота — ожидаемый верхнеуровневый профиль, а не жёсткий лимит:

| Частота | Ожидание |
|---|---|
| Редко | Ошибка или административная/служебная ветка |
| Раунд | Несколько вызовов за раунд |
| Игрок | Несколько вызовов на игрока за раунд |
| Часто | Десятки вызовов в секунду при активной игре |
| Горячий путь | На попадание, урон или экономическую награду; возможны сотни вызовов в секунду |

Нагрузка оценивает допустимый бюджет **одного** обработчика:

| Нагрузка | Рекомендация |
|---|---|
| Низкая | Короткая проверка, изменение контекста, запись в память |
| Средняя | Небольшой расчёт или подготовка отложенной работы |
| Высокая | Только O(1), без LINQ по большим коллекциям, файлов, БД, HTTP и ожиданий |

Риск показывает последствия некорректного обработчика:

| Риск | Что может произойти |
|---|---|
| Низкий | Потеря вторичного уведомления или телеметрии |
| Средний | Неверный визуал, статистика или локальная механика |
| Высокий | Отмена/подмена роли, раунда, покупки, сущности или баланса |
| Критический | Лаг игрового потока, массовая ошибка урона/денег либо рассинхронизация persistent и игрового состояния |
