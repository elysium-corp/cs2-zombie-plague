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

## ZombiePlague.Api

### Players

| Событие | Контекст | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Players.Infecting` | `PlayerInfectingContext` | `PlayerManager.TryInfect`, после первичной проверки цели и до создания роли зомби | Игрок | Низкая | Высокий: отмена или подмена `Player`/`Infector` меняет заражение |
| `Events.Players.Infected` | `PlayerInfectedContext` | После установки роли зомби и показа эффекта заражения | Игрок | Низкая | Средний: подходит для наград и статистики; не запускать повторное заражение синхронно |
| `Events.Players.Disinfecting` | `PlayerDisinfectingContext` | `PlayerManager.TryDisinfect`, до создания человеческой роли | Игрок | Низкая | Высокий: отмена оставляет роль зомби |
| `Events.Players.Disinfected` | `PlayerDisinfectedContext` | После успешной замены роли на человеческую | Игрок | Низкая | Средний: роль уже применена |
| `Events.Players.Humanizing` | `PlayerHumanizingContext` | `PlayerManager.TrySetHuman`, до создания обычной человеческой роли | Игрок | Средняя | Высокий: во время подготовки вызывается для всех игроков; обработчик должен быть O(1) |
| `Events.Players.Humanized` | `PlayerHumanizedContext` | После успешного назначения обычной человеческой роли | Игрок | Средняя | Средний: возможна серия вызовов на старте подготовки |
| `Events.Players.BecomingNemesis` | `PlayerBecomingNemesisContext` | `PlayerManager.TrySetNemesis`, до создания специальной роли | Раунд | Низкая | Высокий: отмена может сорвать сценарий специального раунда |
| `Events.Players.BecameNemesis` | `PlayerBecameNemesisContext` | После установки роли немезиса | Раунд | Низкая | Средний: роль уже активна |
| `Events.Players.BecomingSurvivor` | `PlayerBecomingSurvivorContext` | `PlayerManager.TrySetSurvivor`, до создания специальной роли | Раунд | Низкая | Высокий: отмена может сорвать сценарий специального раунда |
| `Events.Players.BecameSurvivor` | `PlayerBecameSurvivorContext` | После установки роли выжившего | Раунд | Низкая | Средний: роль уже активна |
| `Events.Players.Respawning` | `PlayerRespawningContext` | `PlayerManager.TryRespawn`, после проверки смерти и наличия роли, до `Respawn()` | Игрок | Низкая | Высокий: подменённый игрок повторно валидируется |
| `Events.Players.Respawned` | `PlayerRespawnedContext` | Сразу после вызова `Respawn()` | Игрок | Низкая | Средний: движок может завершать часть spawn-логики позднее |
| `Events.Players.ApplyingRole` | `PlayerApplyingRoleContext` | `PlayerManager.TryApplyRole`, до `Unbind`, смены команды и `Bind` | Игрок | Низкая | Высокий: влияет на команду, класс и способности |
| `Events.Players.RoleApplied` | `PlayerRoleAppliedContext` | После смены команды и привязки текущей роли | Игрок | Низкая | Средний: не вызывать рекурсивно `TryApplyRole` |
| `Events.Players.DeactivatingRole` | `PlayerDeactivatingRoleContext` | `PlayerManager.TryDeactivateRole`, до `Unbind` | Игрок | Низкая | Высокий: отмена сохраняет эффекты роли |
| `Events.Players.RoleDeactivated` | `PlayerRoleDeactivatedContext` | После `Unbind` текущей роли | Игрок | Низкая | Средний: запись роли остаётся в менеджере |

### Classes

| Событие | Контекст | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Classes.Selecting` | `ClassSelectingContext` | `PlayerRepository.SetZClassId/SetHClassId`, до доступа к persistent-сессии | Игрок | Низкая | Высокий: можно отменить или заменить игрока/идентификатор класса; существование класса проверяет вызывающий UI |
| `Events.Classes.Selected` | `ClassSelectedContext` | После записи предпочтения в runtime-сессию | Игрок | Низкая | Средний: сохранение в БД выполняется позднее при lifecycle-сохранении |
| `Events.Classes.SelectionRejected` | `ClassSelectionRejectedContext` | При отмене, пустом идентификаторе или отсутствии сессии | Редко | Низкая | Низкий: runtime-предпочтение не изменено |

### Rounds

| Событие | Контекст | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Rounds.Preparing` | `RoundPreparingContext` | В начале `RoundManager.Prepare`, до завершения активного режима | Раунд | Низкая | Высокий: отмена не запускает подготовку |
| `Events.Rounds.Prepared` | `RoundPreparedContext` | После назначения людей и запуска таймера обратного отсчёта | Раунд | Низкая | Низкий: уведомление о запущенном countdown |
| `Events.Rounds.Starting` | `RoundStartingContext` | `RoundManager.StartRound`, до остановки подготовки и `TryStart` | Раунд | Низкая | Высокий: можно отменить или заменить `RoundId`; неизвестная замена игнорируется |
| `Events.Rounds.Started` | `RoundStartedContext` | После успешного запуска выбранного режима или fallback `infection` | Раунд | Низкая | Средний: `Round` содержит фактически запущенный режим |
| `Events.Rounds.StartRejected` | `RoundStartRejectedContext` | На ветках `NotPreparing`, `CannotStart` и отмены `Starting` | Редко | Низкая | Низкий: только аудит ожидаемого отказа |
| `Events.Rounds.StartFailed` | `RoundStartFailedContext` | В `TryStartRoundInternal`, когда `Round.TryStart()` выбрасывает исключение | Редко | Низкая | Высокий: исключение будет выброшено повторно; обработчик не должен скрывать восстановление |
| `Events.Rounds.Ending` | `RoundEndingContext` | `RoundManager.End`, до остановки подготовки и `Round.End()` | Раунд | Низкая | Высокий: отмена оставляет активный режим |
| `Events.Rounds.Ended` | `RoundEndedContext` | После `Round.End()` и очистки `CurrentRound` | Раунд | Низкая | Средний: состояние режима уже очищено |
| `Events.Rounds.Scheduling` | `RoundSchedulingContext` | `SelectNextRound`, до записи `NextRound` | Редко | Низкая | Высокий: отмена не меняет текущую очередь |
| `Events.Rounds.Scheduled` | `RoundScheduledContext` | После записи `NextRound` | Редко | Низкая | Низкий: уведомление об очереди |
| `Events.Rounds.ScheduleClearing` | `RoundScheduleClearingContext` | `ClearNextRound`, если режим был выбран, до очистки | Редко | Низкая | Высокий: отмена сохраняет выбранный режим |
| `Events.Rounds.ScheduleCleared` | `RoundScheduleClearedContext` | После очистки `NextRound` | Редко | Низкая | Низкий: содержит удалённый режим |

### Combat

| Событие | Контекст | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Combat.KnockbackApplying` | `KnockbackApplyingContext` | `KnockbackService.TryApplyKnockback`, после расчёта скорости и до `Teleport` | Горячий путь | Высокая | Критический: только O(1); неверная `Velocity` ломает движение/физику |
| `Events.Combat.KnockbackApplied` | `KnockbackAppliedContext` | После `Teleport` и постановки таймера восстановления скорости | Горячий путь | Высокая | Критический: нельзя выполнять I/O или тяжёлую телеметрию синхронно |

## CustomEquipment.Api

### Items

| Событие | Контекст | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Items.Purchasing` | `ItemPurchasingContext` | `EquipmentMenu.BuyItem`, до проверки роли и списания денег | Игрок | Низкая | Высокий: можно отменить или подменить игрока/предмет |
| `Events.Items.PaymentCommitted` | `ItemPaymentCommittedContext` | Сразу после успешного `Economy.TrySpendMoney`, до постановки выдачи | Игрок | Низкая | Высокий: деньги уже списаны, предмет ещё не выдан |
| `Events.Items.Purchased` | `ItemPurchasedContext` | После того как `TryGiveItem` принял выдачу; для гранаты фактическое прикрепление может завершиться на следующем world update | Игрок | Низкая | Высокий: не считать это гарантией завершённой асинхронной выдачи; для этого есть `Items.Given` |
| `Events.Items.PurchaseRejected` | `ItemPurchaseRejectedContext` | При отмене, недействительном игроке, запрете роли, отказе оплаты или выдачи | Игрок | Низкая | Низкий: баланс не менялся либо уже запущен возврат |
| `Events.Items.PaymentRefunded` | `ItemPaymentRefundedContext` | После вызова возврата денег, если `TryGiveItem` синхронно отклонил выдачу | Редко | Низкая | Высокий: обработчики экономики способны изменить/отменить возврат; проверять её `Transactions` |
| `Events.Items.Giving` | `ItemGivingContext` | `EquipmentService.TryGiveItem`, после создания экземпляра и до проверки конкретного типа | Игрок | Низкая | Высокий: отмена/подмена меняет выдачу |
| `Events.Items.Given` | `ItemGivenContext` | Из callback `ItemGiver` после фактического прикрепления/применения предмета | Игрок | Средняя | Высокий: для гранаты вызывается на следующем world update, для других типов может быть синхронным |
| `Events.Items.GiveRejected` | `ItemGiveRejectedContext` | На ожидаемых ветках отказа `TryGiveItem` | Игрок | Низкая | Низкий: выдача не была поставлена в очередь |
| `Events.Items.GiveFailed` | `ItemGiveFailedContext` | При исключении создания предмета или постановки выдачи | Редко | Низкая | Высокий: исключение будет выброшено повторно |

### Weapons

| Событие | Контекст | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Weapons.Giving` | `WeaponGivingContext` | После `Items.Giving`, перед выдачей оружия | Игрок | Низкая | Высокий: типовая отмена отклоняет всю выдачу |
| `Events.Weapons.Given` | `WeaponGivenContext` | После прикрепления оружия и регистрации в runtime-каталоге | Игрок | Низкая | Средний: оружие уже доступно игроку |
| `Events.Weapons.DamageModifying` | `WeaponDamageModifyingContext` | На `TakeDamage.Pre`, после штатного расчёта множителя и до записи урона | Горячий путь | Высокая | Критический: только O(1); отмена оставляет базовый урон, подписчик обязан не задавать NaN/Infinity/отрицательное значение |
| `Events.Weapons.DamageModified` | `WeaponDamageModifiedContext` | После записи модифицированного урона в damage info | Горячий путь | Высокая | Критический: БД, HTTP, логирование каждого попадания запрещены |
| `Events.Weapons.ImpactProcessing` | `WeaponImpactProcessingContext` | `OnBulletImpactPost`, до создания tracer/muzzle/impact particles | Горячий путь | Высокая | Критический: отмена отключает пользовательские частицы этого попадания |
| `Events.Weapons.ImpactProcessed` | `WeaponImpactProcessedContext` | После создания настроенных частиц попадания | Горячий путь | Высокая | Критический: событие вызывается даже если для оружия не настроен отдельный тип частицы |
| `Events.Weapons.AmmoPurchasing` | `WeaponAmmoPurchasingContext` | По нажатию `E` с активным магазинным оружием, до проверки лимита и оплаты | Игрок | Низкая | Высокий: цена и количество изменяемы; значения валидируются |
| `Events.Weapons.AmmoPurchased` | `WeaponAmmoPurchasedContext` | После оплаты и обновления reserve ammo | Игрок | Низкая | Средний: содержит фактически добавленное число патронов с учётом лимита |
| `Events.Weapons.AmmoPurchaseRejected` | `WeaponAmmoPurchaseRejectedContext` | При отсутствии настройки, полном запасе, отмене, неверных значениях или отказе оплаты | Игрок | Низкая | Низкий: боеприпасы не изменены |

### Grenades

| Событие | Контекст | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Grenades.Giving` | `GrenadeGivingContext` | После `Items.Giving`, перед выдачей гранаты | Игрок | Низкая | Высокий: типовая отмена отклоняет выдачу |
| `Events.Grenades.Given` | `GrenadeGivenContext` | После поиска и прикрепления сущности гранаты на следующем world update | Игрок | Средняя | Средний: фактическая выдача завершена |
| `Events.Grenades.Throwing` | `GrenadeThrowingContext` | После создания projectile и определения пользовательской гранаты, до установки модели | Часто | Средняя | Высокий: отмена не удаляет projectile, а отключает его пользовательскую регистрацию |
| `Events.Grenades.Thrown` | `GrenadeThrownContext` | После установки модели и регистрации броска | Часто | Средняя | Средний: используется контроллером детонации |
| `Events.Grenades.ThrowRejected` | `GrenadeThrowRejectedContext` | При отмене `Throwing` или недействительном projectile | Редко | Низкая | Низкий: только аудит отказа |
| `Events.Grenades.Detonating` | `GrenadeDetonatingContext` | Перед удалением projectile и вызовом пользовательской детонации | Часто | Средняя | Высокий: отмена оставляет штатную дальнейшую судьбу projectile |
| `Events.Grenades.Detonated` | `GrenadeDetonatedContext` | После `OnDetonate` пользовательской гранаты | Часто | Средняя | Средний: эффекты и урон уже созданы |
| `Events.Grenades.DetonationRejected` | `GrenadeDetonationRejectedContext` | При отмене, неверной подмене, недействительном projectile или thrower | Редко | Низкая | Низкий: пользовательская логика не выполнена |

### Mines

| Событие | Контекст | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Mines.Placing` | `MinePlacingContext` | После проверки поверхности и создания сущности, до `Spawn` | Игрок | Низкая | Высокий: отмена вызывает возврат цены |
| `Events.Mines.Placed` | `MinePlacedContext` | После `LaserMineEntity.Spawn` | Игрок | Низкая | Средний: владелец затем регистрируется внутренним подписчиком |
| `Events.Mines.PlacementRejected` | `MinePlacementRejectedContext` | При неподходящей поверхности, отмене или недействительном игроке | Игрок | Низкая | Низкий: модуль запускает возврат цены |

## SupplyBox.Api

| Событие | Контекст | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Spawning` | `SupplyBoxSpawningContext` | `SupplyBox.SpawnSupplyBox`, после проверок режима/шанса/лимита и до выбора точки | Раунд | Низкая | Высокий: отмена пропускает текущую попытку создания |
| `Events.Spawned` | `SupplyBoxSpawnedContext` | После создания сущности и добавления в список активных ящиков | Раунд | Низкая | Средний: ящик ещё спускается |
| `Events.SpawnRejected` | `SupplyBoxSpawnRejectedContext` | На всех ожидаемых ветках отказа: режим, лимит, шанс, отмена, отсутствие точки | Раунд | Низкая | Низкий: полезно для диагностики конфигурации |
| `Events.Landed` | `SupplyBoxLandedContext` | Один раз в `DropThinker`, когда ящик достиг целевой высоты | Раунд | Низкая | Средний: callback выполняется из scheduler игрового потока |
| `Events.Collecting` | `SupplyBoxCollectingContext` | При контакте допустимого игрока с ящиком, до удаления сущностей | Игрок | Средняя | Высокий: проверка близости идёт каждые 0,05 с, но событие вызывается только для кандидата на сбор |
| `Events.Collected` | `SupplyBoxCollectedContext` | После удаления сущностей и остановки thinkers | Игрок | Низкая | Средний: внутренний подписчик удаляет ящик из active-list |
| `Events.CollectionRejected` | `SupplyBoxCollectionRejectedContext` | При отмене, неверной подмене, недействительном игроке или отмене уничтожения | Редко | Низкая | Низкий: ящик остаётся доступным |
| `Events.Destroying` | `SupplyBoxDestroyingContext` | Перед `Despawn` ящика/парашюта и отменой thinkers | Игрок | Низкая | Высокий: отмена прерывает сбор и сохраняет сущности |
| `Events.Destroyed` | `SupplyBoxDestroyedContext` | После `Despawn` и отмены thinkers | Игрок | Низкая | Средний: `Collected` отправляется сразу после него |

## Economy.Api

| Событие | Контекст | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Transactions.Processing` | `EconomyTransactionProcessingContext` | `GiveMoney` и `TrySpendMoney`, после проверки входного аргумента и до доступа к сессии | Горячий путь | Высокая | Критический: награда за каждый урон проходит здесь; только O(1), сумма и игрок изменяемы |
| `Events.Transactions.Committed` | `EconomyTransactionCommittedContext` | После атомарного изменения session balance и обновления CS2 money projection | Горячий путь | Высокая | Критический: persistent-состояние уже изменено; не запускать синхронный I/O |
| `Events.Transactions.Rejected` | `EconomyTransactionRejectedContext` | При отмене, отсутствии/незагрузившемся счёте, нехватке средств или лимите | Часто | Высокая | Высокий: только наблюдение; повтор операции из обработчика может создать рекурсию |
| `Events.Transactions.Failed` | `EconomyTransactionFailedContext` | Если обновление CS2 money projection выбросило исключение после изменения session balance | Редко | Низкая | Критический: возможна временная рассинхронизация; исключение повторно выбрасывается |

### Accounts

События `Loaded`, `LoadFailed`, `Saved` и `SaveFailed` выполняются из фоновой очереди БД. В их обработчиках нельзя обращаться к игровым entity/API без явного возврата в scheduler игрового потока.

| Событие | Контекст | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Accounts.Initialized` | `EconomyAccountInitializedContext` | После создания runtime-сессии и применения стартового баланса игроку, до фоновой загрузки | Игрок | Низкая | Средний: загруженный баланс ещё неизвестен |
| `Events.Accounts.Loaded` | `EconomyAccountLoadedContext` | В фоновой очереди после загрузки/создания записи и merge локальной дельты | Игрок | Низкая | Высокий: не игровой поток; game projection существующего счёта обновляется позднее через scheduler |
| `Events.Accounts.LoadFailed` | `EconomyAccountLoadFailedContext` | В фоновой очереди при исключении загрузки | Редко | Низкая | Высокий: не игровой поток; исключение повторно передаётся tracker-у задач |
| `Events.Accounts.Removed` | `EconomyAccountRemovedContext` | После удаления runtime-сессии при disconnect/unload, перед постановкой сохранения | Игрок | Низкая | Средний: счёт больше недоступен через API, сохранение ещё может завершиться ошибкой |
| `Events.Accounts.Saved` | `EconomyAccountSavedContext` | В фоновой очереди после записи dirty snapshot и `MarkSaved` | Игрок | Низкая | Высокий: не игровой поток; не хранить ссылки на игроков/entity |
| `Events.Accounts.SaveFailed` | `EconomyAccountSaveFailedContext` | В фоновой очереди при исключении сохранения | Редко | Низкая | Высокий: runtime-сессия уже удалена; требуется внешняя диагностика/повтор инфраструктуры |

## Правила для обработчиков

1. На событиях с нагрузкой `Высокая` не выполнять запросы к БД/сети, чтение файлов, синхронное ожидание задач и подробное логирование каждого вызова. Скопируйте минимальные примитивные данные в bounded queue и обрабатывайте их отдельно.
2. Не хранить `IPlayer`, entity/projectile и другие игровые объекты для поздней работы без повторной проверки `IsValid`, `SessionId` и принадлежности текущей карте.
3. Не использовать `async void`: dispatcher не сможет дождаться такой работы или изолировать исключение после первого `await`.
4. Не вызывать из обработчика ту же операцию без guard — это создаёт рекурсивный dispatch.
5. Всегда отписываться при выгрузке плагина. Для одного и того же delegate каждый `Unhook` удаляет последнюю соответствующую регистрацию.
6. В `...ing` проверять `IsCancelled`, если обработчик с низким приоритетом должен только наблюдать. Не пытаться «разотменить» контекст: контракт этого не предоставляет.
