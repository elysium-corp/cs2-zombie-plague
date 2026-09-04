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

| Событие | Контекст и параметры | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Players.Infecting` | `PlayerInfectingContext`<br>`Player: IPlayer` (mutable)<br>`Infector: IPlayer?` (mutable) (nullable)<br>cancellable | `PlayerManager.TryInfect`, после первичной проверки цели и до создания роли зомби | Игрок | Низкая | Высокий: отмена или подмена `Player`/`Infector` меняет заражение |
| `Events.Players.Infected` | `PlayerInfectedContext`<br>`Player: IPlayer`<br>`Infector: IPlayer?` (nullable) | После установки роли зомби и показа эффекта заражения | Игрок | Низкая | Средний: подходит для наград и статистики; не запускать повторное заражение синхронно |
| `Events.Players.Disinfecting` | `PlayerDisinfectingContext`<br>`Player: IPlayer` (mutable)<br>cancellable | `PlayerManager.TryDisinfect`, до создания человеческой роли | Игрок | Низкая | Высокий: отмена оставляет роль зомби |
| `Events.Players.Disinfected` | `PlayerDisinfectedContext`<br>`Player: IPlayer` | После успешной замены роли на человеческую | Игрок | Низкая | Средний: роль уже применена |
| `Events.Players.Humanizing` | `PlayerHumanizingContext`<br>`Player: IPlayer` (mutable)<br>cancellable | `PlayerManager.TrySetHuman`, до создания обычной человеческой роли | Игрок | Средняя | Высокий: во время подготовки вызывается для всех игроков; обработчик должен быть O(1) |
| `Events.Players.Humanized` | `PlayerHumanizedContext`<br>`Player: IPlayer` | После успешного назначения обычной человеческой роли | Игрок | Средняя | Средний: возможна серия вызовов на старте подготовки |
| `Events.Players.BecomingNemesis` | `PlayerBecomingNemesisContext`<br>`Player: IPlayer` (mutable)<br>cancellable | `PlayerManager.TrySetNemesis`, до создания специальной роли | Раунд | Низкая | Высокий: отмена может сорвать сценарий специального раунда |
| `Events.Players.BecameNemesis` | `PlayerBecameNemesisContext`<br>`Player: IPlayer` | После установки роли немезиса | Раунд | Низкая | Средний: роль уже активна |
| `Events.Players.BecomingSurvivor` | `PlayerBecomingSurvivorContext`<br>`Player: IPlayer` (mutable)<br>cancellable | `PlayerManager.TrySetSurvivor`, до создания специальной роли | Раунд | Низкая | Высокий: отмена может сорвать сценарий специального раунда |
| `Events.Players.BecameSurvivor` | `PlayerBecameSurvivorContext`<br>`Player: IPlayer` | После установки роли выжившего | Раунд | Низкая | Средний: роль уже активна |
| `Events.Players.Respawning` | `PlayerRespawningContext`<br>`Player: IPlayer` (mutable)<br>cancellable | `PlayerManager.TryRespawn`, после проверки смерти и наличия роли, до `Respawn()` | Игрок | Низкая | Высокий: подменённый игрок повторно валидируется |
| `Events.Players.Respawned` | `PlayerRespawnedContext`<br>`Player: IPlayer` | Сразу после вызова `Respawn()` | Игрок | Низкая | Средний: движок может завершать часть spawn-логики позднее |
| `Events.Players.ApplyingRole` | `PlayerApplyingRoleContext`<br>`Player: IPlayer` (mutable)<br>cancellable | `PlayerManager.TryApplyRole`, до `Unbind`, смены команды и `Bind` | Игрок | Низкая | Высокий: влияет на команду, класс и способности |
| `Events.Players.RoleApplied` | `PlayerRoleAppliedContext`<br>`Player: IPlayer` | После смены команды и привязки текущей роли | Игрок | Низкая | Средний: не вызывать рекурсивно `TryApplyRole` |
| `Events.Players.DeactivatingRole` | `PlayerDeactivatingRoleContext`<br>`Player: IPlayer` (mutable)<br>cancellable | `PlayerManager.TryDeactivateRole`, до `Unbind` | Игрок | Низкая | Высокий: отмена сохраняет эффекты роли |
| `Events.Players.RoleDeactivated` | `PlayerRoleDeactivatedContext`<br>`Player: IPlayer` | После `Unbind` текущей роли | Игрок | Низкая | Средний: запись роли остаётся в менеджере |

### Classes

| Событие | Контекст и параметры | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Classes.Selecting` | `ClassSelectingContext`<br>`Player: IPlayer` (mutable)<br>`OriginalClassId: string`<br>`ClassId: string` (mutable)<br>`Kind: PlayerClassKind`<br>cancellable | `PlayerRepository.SetZClassId/SetHClassId`, до доступа к persistent-сессии | Игрок | Низкая | Высокий: можно отменить или заменить игрока/идентификатор класса; существование класса проверяет вызывающий UI |
| `Events.Classes.Selected` | `ClassSelectedContext`<br>`Player: IPlayer`<br>`ClassId: string`<br>`Kind: PlayerClassKind` | После записи предпочтения в runtime-сессию | Игрок | Низкая | Средний: сохранение в БД выполняется позднее при lifecycle-сохранении |
| `Events.Classes.SelectionRejected` | `ClassSelectionRejectedContext`<br>`Player: IPlayer`<br>`ClassId: string`<br>`Kind: PlayerClassKind`<br>`Reason: ClassSelectionRejectionReason` | При отмене, пустом идентификаторе или отсутствии сессии | Редко | Низкая | Низкий: runtime-предпочтение не изменено |

### Rounds

| Событие | Контекст и параметры | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Rounds.Preparing` | `RoundPreparingContext`<br><br>cancellable | В начале `RoundManager.Prepare`, до завершения активного режима | Раунд | Низкая | Высокий: отмена не запускает подготовку |
| `Events.Rounds.Prepared` | `RoundPreparedContext`<br>`DelaySeconds: int` | После назначения людей и запуска таймера обратного отсчёта | Раунд | Низкая | Низкий: уведомление о запущенном countdown |
| `Events.Rounds.Starting` | `RoundStartingContext`<br>`OriginalRoundId: string`<br>`RoundId: string` (mutable)<br>cancellable | `RoundManager.StartRound`, до остановки подготовки и `TryStart` | Раунд | Низкая | Высокий: можно отменить или заменить `RoundId`; неизвестная замена игнорируется |
| `Events.Rounds.Started` | `RoundStartedContext`<br>`Round: IRound` | После успешного запуска выбранного режима или fallback `infection` | Раунд | Низкая | Средний: `Round` содержит фактически запущенный режим |
| `Events.Rounds.StartRejected` | `RoundStartRejectedContext`<br>`RoundId: string?` (nullable)<br>`Reason: RoundStartRejectionReason` | На ветках `NotPreparing`, `CannotStart` и отмены `Starting` | Редко | Низкая | Низкий: только аудит ожидаемого отказа |
| `Events.Rounds.StartFailed` | `RoundStartFailedContext`<br>`Round: IRound`<br>`Exception: Exception` | В `TryStartRoundInternal`, когда `Round.TryStart()` выбрасывает исключение | Редко | Низкая | Высокий: исключение будет выброшено повторно; обработчик не должен скрывать восстановление |
| `Events.Rounds.Ending` | `RoundEndingContext`<br>`Round: IRound`<br>cancellable | `RoundManager.End`, до остановки подготовки и `Round.End()` | Раунд | Низкая | Высокий: отмена оставляет активный режим |
| `Events.Rounds.Ended` | `RoundEndedContext`<br>`Round: IRound` | После `Round.End()` и очистки `CurrentRound` | Раунд | Низкая | Средний: состояние режима уже очищено |
| `Events.Rounds.Scheduling` | `RoundSchedulingContext`<br>`Round: IRound`<br>cancellable | `SelectNextRound`, до записи `NextRound` | Редко | Низкая | Высокий: отмена не меняет текущую очередь |
| `Events.Rounds.Scheduled` | `RoundScheduledContext`<br>`Round: IRound` | После записи `NextRound` | Редко | Низкая | Низкий: уведомление об очереди |
| `Events.Rounds.ScheduleClearing` | `RoundScheduleClearingContext`<br>`Round: IRound`<br>cancellable | `ClearNextRound`, если режим был выбран, до очистки | Редко | Низкая | Высокий: отмена сохраняет выбранный режим |
| `Events.Rounds.ScheduleCleared` | `RoundScheduleClearedContext`<br>`Round: IRound` | После очистки `NextRound` | Редко | Низкая | Низкий: содержит удалённый режим |

### Combat

| Событие | Контекст и параметры | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Combat.KnockbackApplying` | `KnockbackApplyingContext`<br>`Attacker: IPlayer`<br>`Victim: IPlayer`<br>`Data: KnockbackData`<br>`Velocity: Vector` (mutable)<br>cancellable | `KnockbackService.TryApplyKnockback`, после расчёта скорости и до `Teleport` | Горячий путь | Высокая | Критический: только O(1); неверная `Velocity` ломает движение/физику |
| `Events.Combat.KnockbackApplied` | `KnockbackAppliedContext`<br>`Attacker: IPlayer`<br>`Victim: IPlayer`<br>`Data: KnockbackData`<br>`Velocity: Vector` | После `Teleport` и постановки таймера восстановления скорости | Горячий путь | Высокая | Критический: нельзя выполнять I/O или тяжёлую телеметрию синхронно |

## CustomEquipment.Api

### Items

| Событие | Контекст и параметры | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Items.Purchasing` | `ItemPurchasingContext`<br>`Player: IPlayer` (mutable)<br>`Item: IShopItem` (mutable)<br>cancellable | Не вызывается начиная с CustomEquipment.Core 0.6.0; используйте `Shop.Api.IShopApi.Events.Purchasing` | Игрок | Низкая | Высокий: можно отменить или подменить игрока/предмет |
| `Events.Items.PaymentCommitted` | `ItemPaymentCommittedContext`<br>`Player: IPlayer`<br>`Item: IShopItem`<br>`Amount: int` | Не вызывается начиная с CustomEquipment.Core 0.6.0; оплатой управляет Shop.Core | Игрок | Низкая | Высокий: деньги уже списаны, предмет ещё не выдан |
| `Events.Items.Purchased` | `ItemPurchasedContext`<br>`Player: IPlayer`<br>`Item: IShopItem` | Не вызывается начиная с CustomEquipment.Core 0.6.0; используйте `Shop.Api.IShopApi.Events.Purchased` | Игрок | Низкая | Высокий: не считать это гарантией завершённой асинхронной выдачи; для этого есть `Items.Given` |
| `Events.Items.PurchaseRejected` | `ItemPurchaseRejectedContext`<br>`Player: IPlayer`<br>`Item: IShopItem`<br>`Reason: ItemPurchaseRejectionReason` | Не вызывается начиная с CustomEquipment.Core 0.6.0; используйте `Shop.Api.IShopApi.Events.PurchaseRejected` | Игрок | Низкая | Низкий: баланс не менялся либо уже запущен возврат |
| `Events.Items.PaymentRefunded` | `ItemPaymentRefundedContext`<br>`Player: IPlayer`<br>`Item: IShopItem`<br>`Amount: int` | Не вызывается начиная с CustomEquipment.Core 0.6.0; возвратом управляет Shop.Core | Редко | Низкая | Высокий: обработчики экономики способны изменить/отменить возврат; проверять её `Transactions` |
| `Events.Items.Giving` | `ItemGivingContext`<br>`Player: IPlayer` (mutable)<br>`Item: IItem` (mutable)<br>`Action: GiveAction` (mutable)<br>cancellable | `EquipmentService.TryGiveItem`, после создания экземпляра и до проверки конкретного типа | Игрок | Низкая | Высокий: отмена/подмена меняет выдачу |
| `Events.Items.Given` | `ItemGivenContext`<br>`Player: IPlayer`<br>`Item: IItem`<br>`Action: GiveAction` | Из callback `ItemGiver` после фактического прикрепления/применения предмета | Игрок | Средняя | Высокий: для гранаты вызывается на следующем world update, для других типов может быть синхронным |
| `Events.Items.GiveRejected` | `ItemGiveRejectedContext`<br>`Player: IPlayer`<br>`InternalName: string`<br>`Item: IItem?` (nullable)<br>`Action: GiveAction`<br>`Reason: ItemGiveRejectionReason` | На ожидаемых ветках отказа `TryGiveItem` | Игрок | Низкая | Низкий: выдача не была поставлена в очередь |
| `Events.Items.GiveFailed` | `ItemGiveFailedContext`<br>`Player: IPlayer`<br>`InternalName: string`<br>`Action: GiveAction`<br>`Exception: Exception` | При исключении создания предмета или постановки выдачи | Редко | Низкая | Высокий: исключение будет выброшено повторно |

### Weapons

| Событие | Контекст и параметры | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Weapons.Giving` | `WeaponGivingContext`<br>`Player: IPlayer` (mutable)<br>`Weapon: IWeapon` (mutable)<br>`Action: GiveAction` (mutable)<br>cancellable | После `Items.Giving`, перед выдачей оружия | Игрок | Низкая | Высокий: типовая отмена отклоняет всю выдачу |
| `Events.Weapons.Given` | `WeaponGivenContext`<br>`Player: IPlayer`<br>`Weapon: IWeapon`<br>`Action: GiveAction` | После прикрепления оружия и регистрации в runtime-каталоге | Игрок | Низкая | Средний: оружие уже доступно игроку |
| `Events.Weapons.DamageModifying` | `WeaponDamageModifyingContext`<br>`Attacker: IPlayer`<br>`Victim: IPlayer`<br>`Weapon: IWeapon`<br>`OriginalDamage: float`<br>`Damage: float` (mutable)<br>cancellable | На `TakeDamage.Pre`, после штатного расчёта множителя и до записи урона | Горячий путь | Высокая | Критический: только O(1); отмена оставляет базовый урон, подписчик обязан не задавать NaN/Infinity/отрицательное значение |
| `Events.Weapons.DamageModified` | `WeaponDamageModifiedContext`<br>`Attacker: IPlayer`<br>`Victim: IPlayer`<br>`Weapon: IWeapon`<br>`OriginalDamage: float`<br>`Damage: float` | После записи модифицированного урона в damage info | Горячий путь | Высокая | Критический: БД, HTTP, логирование каждого попадания запрещены |
| `Events.Weapons.ImpactProcessing` | `WeaponImpactProcessingContext`<br>`Player: IPlayer`<br>`Weapon: IWeapon`<br>`Position: Vector` (mutable)<br>cancellable | `OnBulletImpactPost`, до создания tracer/muzzle/impact particles | Горячий путь | Высокая | Критический: отмена отключает пользовательские частицы этого попадания |
| `Events.Weapons.ImpactProcessed` | `WeaponImpactProcessedContext`<br>`Player: IPlayer`<br>`Weapon: IWeapon`<br>`Position: Vector` | После создания настроенных частиц попадания | Горячий путь | Высокая | Критический: событие вызывается даже если для оружия не настроен отдельный тип частицы |
| `Events.Weapons.AmmoPurchasing` | `WeaponAmmoPurchasingContext`<br>`Player: IPlayer`<br>`Weapon: IWeapon`<br>`Price: int` (mutable)<br>`Amount: int` (mutable)<br>cancellable | Не вызывается начиная с CustomEquipment.Core 0.6.0; докупкой по `E` управляет Shop.Core | Игрок | Низкая | Высокий: цена и количество изменяемы; значения валидируются |
| `Events.Weapons.AmmoPurchased` | `WeaponAmmoPurchasedContext`<br>`Player: IPlayer`<br>`Weapon: IWeapon`<br>`Price: int`<br>`Amount: int`<br>`ReserveAmmo: int` | Не вызывается начиная с CustomEquipment.Core 0.6.0; используйте `Shop.Api.IShopApi.Events.AmmoPurchased` | Игрок | Низкая | Средний: содержит фактически добавленное число патронов с учётом лимита |
| `Events.Weapons.AmmoPurchaseRejected` | `WeaponAmmoPurchaseRejectedContext`<br>`Player: IPlayer`<br>`Weapon: IWeapon`<br>`Reason: WeaponAmmoPurchaseRejectionReason` | Не вызывается начиная с CustomEquipment.Core 0.6.0; используйте `Shop.Api.IShopApi.Events.PurchaseRejected` | Игрок | Низкая | Низкий: боеприпасы не изменены |

### Grenades

| Событие | Контекст и параметры | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Grenades.Giving` | `GrenadeGivingContext`<br>`Player: IPlayer` (mutable)<br>`Grenade: IGrenade` (mutable)<br>`Action: GiveAction` (mutable)<br>cancellable | После `Items.Giving`, перед выдачей гранаты | Игрок | Низкая | Высокий: типовая отмена отклоняет выдачу |
| `Events.Grenades.Given` | `GrenadeGivenContext`<br>`Player: IPlayer`<br>`Grenade: IGrenade`<br>`Action: GiveAction` | После поиска и прикрепления сущности гранаты на следующем world update | Игрок | Средняя | Средний: фактическая выдача завершена |
| `Events.Grenades.Throwing` | `GrenadeThrowingContext`<br>`Grenade: IGrenade` (mutable)<br>`Projectile: CBaseCSGrenadeProjectile` (mutable)<br>cancellable | После создания projectile и определения пользовательской гранаты, до установки модели | Часто | Средняя | Высокий: отмена не удаляет projectile, а отключает его пользовательскую регистрацию |
| `Events.Grenades.Thrown` | `GrenadeThrownContext`<br>`Grenade: IGrenade`<br>`Projectile: CBaseCSGrenadeProjectile` | После установки модели и регистрации броска | Часто | Средняя | Средний: используется контроллером детонации |
| `Events.Grenades.ThrowRejected` | `GrenadeThrowRejectedContext`<br>`Grenade: IGrenade`<br>`Projectile: CBaseCSGrenadeProjectile`<br>`Reason: GrenadeThrowRejectionReason` | При отмене `Throwing` или недействительном projectile | Редко | Низкая | Низкий: только аудит отказа |
| `Events.Grenades.Detonating` | `GrenadeDetonatingContext`<br>`Grenade: IGrenade` (mutable)<br>`Projectile: CBaseCSGrenadeProjectile` (mutable)<br>`Position: Vector` (mutable)<br>cancellable | Перед удалением projectile и вызовом пользовательской детонации | Часто | Средняя | Высокий: отмена оставляет штатную дальнейшую судьбу projectile |
| `Events.Grenades.Detonated` | `GrenadeDetonatedContext`<br>`Grenade: IGrenade`<br>`Projectile: CBaseCSGrenadeProjectile`<br>`Position: Vector` | После `OnDetonate` пользовательской гранаты | Часто | Средняя | Средний: эффекты и урон уже созданы |
| `Events.Grenades.DetonationRejected` | `GrenadeDetonationRejectedContext`<br>`Grenade: IGrenade`<br>`Projectile: CBaseCSGrenadeProjectile`<br>`Reason: GrenadeDetonationRejectionReason` | При отмене, неверной подмене, недействительном projectile или thrower | Редко | Низкая | Низкий: пользовательская логика не выполнена |

### Mines

| Событие | Контекст и параметры | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Mines.Placing` | `MinePlacingContext`<br>`Player: IPlayer` (mutable)<br>`Mine: LaserMineEntityBase`<br>cancellable | После проверки поверхности и создания сущности, до `Spawn` | Игрок | Низкая | Средний: при отмене предмет остаётся у игрока для повторной установки |
| `Events.Mines.Placed` | `MinePlacedContext`<br>`Player: IPlayer`<br>`Mine: LaserMineEntityBase` | После `LaserMineEntity.Spawn` | Игрок | Низкая | Средний: владелец затем регистрируется внутренним подписчиком |
| `Events.Mines.PlacementRejected` | `MinePlacementRejectedContext`<br>`Player: IPlayer`<br>`Mine: LaserMineEntityBase?` (nullable)<br>`Reason: MinePlacementRejectionReason` | При неподходящей поверхности, отмене или недействительном игроке | Игрок | Низкая | Низкий: предмет остаётся у игрока для повторной установки |

## SupplyBox.Api

| Событие | Контекст и параметры | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Spawning` | `SupplyBoxSpawningContext`<br>`ActiveSupplyBoxes: IReadOnlyCollection<ISupplyBoxEntity>`<br>cancellable | `SupplyBox.SpawnSupplyBox`, после проверок режима/шанса/лимита и до выбора точки | Раунд | Низкая | Высокий: отмена пропускает текущую попытку создания |
| `Events.Spawned` | `SupplyBoxSpawnedContext`<br>`SupplyBox: ISupplyBoxEntity` | После создания сущности и добавления в список активных ящиков | Раунд | Низкая | Средний: ящик ещё спускается |
| `Events.SpawnRejected` | `SupplyBoxSpawnRejectedContext`<br>`Reason: SupplyBoxSpawnRejectionReason` | На всех ожидаемых ветках отказа: режим, лимит, шанс, отмена, отсутствие точки | Раунд | Низкая | Низкий: полезно для диагностики конфигурации |
| `Events.Landed` | `SupplyBoxLandedContext`<br>`SupplyBox: ISupplyBoxEntity` | Один раз в `DropThinker`, когда ящик достиг целевой высоты | Раунд | Низкая | Средний: callback выполняется из scheduler игрового потока |
| `Events.Collecting` | `SupplyBoxCollectingContext`<br>`Player: IPlayer` (mutable)<br>`SupplyBox: ISupplyBoxEntity` (mutable)<br>cancellable | При контакте допустимого игрока с ящиком, до удаления сущностей | Игрок | Средняя | Высокий: проверка близости идёт каждые 0,05 с, но событие вызывается только для кандидата на сбор |
| `Events.Collected` | `SupplyBoxCollectedContext`<br>`Player: IPlayer`<br>`SupplyBox: ISupplyBoxEntity` | После удаления сущностей и остановки thinkers | Игрок | Низкая | Средний: внутренний подписчик удаляет ящик из active-list |
| `Events.CollectionRejected` | `SupplyBoxCollectionRejectedContext`<br>`Player: IPlayer`<br>`SupplyBox: ISupplyBoxEntity`<br>`Reason: SupplyBoxCollectionRejectionReason` | При отмене, неверной подмене, недействительном игроке или отмене уничтожения | Редко | Низкая | Низкий: ящик остаётся доступным |
| `Events.Destroying` | `SupplyBoxDestroyingContext`<br>`SupplyBox: ISupplyBoxEntity`<br>cancellable | Перед `Despawn` ящика/парашюта и отменой thinkers | Игрок | Низкая | Высокий: отмена прерывает сбор и сохраняет сущности |
| `Events.Destroyed` | `SupplyBoxDestroyedContext`<br>`SupplyBox: ISupplyBoxEntity` | После `Despawn` и отмены thinkers | Игрок | Низкая | Средний: `Collected` отправляется сразу после него |

## Economy.Api

| Событие | Контекст и параметры | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Transactions.Processing` | `EconomyTransactionProcessingContext`<br>`OriginalPlayer: IPlayer`<br>`Player: IPlayer` (mutable)<br>`OriginalAmount: int`<br>`Amount: int` (mutable)<br>`Kind: EconomyTransactionKind`<br>cancellable | `GiveMoney` и `TrySpendMoney`, после проверки входного аргумента и до доступа к сессии | Горячий путь | Высокая | Критический: награда за каждый урон проходит здесь; только O(1), сумма и игрок изменяемы |
| `Events.Transactions.Committed` | `EconomyTransactionCommittedContext`<br>`Player: IPlayer`<br>`RequestedAmount: int`<br>`AppliedAmount: int`<br>`PreviousBalance: int`<br>`Balance: int`<br>`Kind: EconomyTransactionKind` | После атомарного изменения session balance и обновления CS2 money projection | Горячий путь | Высокая | Критический: persistent-состояние уже изменено; не запускать синхронный I/O |
| `Events.Transactions.Rejected` | `EconomyTransactionRejectedContext`<br>`Player: IPlayer`<br>`Amount: int`<br>`Kind: EconomyTransactionKind`<br>`Reason: EconomyTransactionRejectionReason` | При отмене, отсутствии/незагрузившемся счёте, нехватке средств или лимите | Часто | Высокая | Высокий: только наблюдение; повтор операции из обработчика может создать рекурсию |
| `Events.Transactions.Failed` | `EconomyTransactionFailedContext`<br>`Player: IPlayer`<br>`Amount: int`<br>`Kind: EconomyTransactionKind`<br>`Exception: Exception` | Если обновление CS2 money projection выбросило исключение после изменения session balance | Редко | Низкая | Критический: возможна временная рассинхронизация; исключение повторно выбрасывается |

### Accounts

События `Loaded`, `LoadFailed`, `Saved` и `SaveFailed` выполняются из фоновой очереди БД. В их обработчиках нельзя обращаться к игровым entity/API без явного возврата в scheduler игрового потока.

| Событие | Контекст и параметры | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Accounts.Initialized` | `EconomyAccountInitializedContext`<br>`Player: IPlayer`<br>`Balance: int` | После создания runtime-сессии и применения стартового баланса игроку, до фоновой загрузки | Игрок | Низкая | Средний: загруженный баланс ещё неизвестен |
| `Events.Accounts.Loaded` | `EconomyAccountLoadedContext`<br>`SteamId: ulong`<br>`Balance: int`<br>`IsNew: bool` | В фоновой очереди после загрузки/создания записи и merge локальной дельты | Игрок | Низкая | Высокий: не игровой поток; game projection существующего счёта обновляется позднее через scheduler |
| `Events.Accounts.LoadFailed` | `EconomyAccountLoadFailedContext`<br>`SteamId: ulong`<br>`Exception: Exception` | В фоновой очереди при исключении загрузки | Редко | Низкая | Высокий: не игровой поток; исключение повторно передаётся tracker-у задач |
| `Events.Accounts.Removed` | `EconomyAccountRemovedContext`<br>`SteamId: ulong`<br>`Balance: int` | После удаления runtime-сессии при disconnect/unload, перед постановкой сохранения | Игрок | Низкая | Средний: счёт больше недоступен через API, сохранение ещё может завершиться ошибкой |
| `Events.Accounts.Saved` | `EconomyAccountSavedContext`<br>`SteamId: ulong`<br>`Balance: int` | В фоновой очереди после записи dirty snapshot и `MarkSaved` | Игрок | Низкая | Высокий: не игровой поток; не хранить ссылки на игроков/entity |
| `Events.Accounts.SaveFailed` | `EconomyAccountSaveFailedContext`<br>`SteamId: ulong`<br>`Exception: Exception` | В фоновой очереди при исключении сохранения | Редко | Низкая | Высокий: runtime-сессия уже удалена; требуется внешняя диагностика/повтор инфраструктуры |

## Shop.Api

| Событие | Контекст и параметры | Когда вызывается | Частота | Нагрузка | Риск и ограничения |
|---|---|---|---|---|---|
| `Events.Purchasing` | `ShopPurchasingContext`<br>`Player: IPlayer` (mutable)<br>`Offer: ShopOffer`<br>`Price: int` (mutable)<br>cancellable | После первой проверки оффера, до повторной проверки, списания и выдачи | Игрок | Низкая | Высокий: отмена, подмена игрока или цены меняет покупку; все значения будут повторно проверены |
| `Events.Purchased` | `ShopPurchasedContext`<br>`Player: IPlayer`<br>`Offer: ShopOffer`<br>`Price: int` | После списания, принятой выдачи и записи лимитов оффера | Игрок | Низкая | Высокий: для гранаты фактическое прикрепление сущности может завершиться на следующем world update |
| `Events.PurchaseRejected` | `ShopPurchaseRejectedContext`<br>`Player: IPlayer`<br>`Offer: ShopOffer?` (nullable)<br>`Reason: ShopAvailabilityReason` | При недоступности, отмене, отказе списания/выдачи или ошибке возврата | Игрок | Низкая | Низкий: событие предназначено для UI, аудита и телеметрии; повторять покупку из обработчика нельзя |
| `Events.AmmoPurchased` | `ShopAmmoPurchasedContext`<br>`Player: IPlayer`<br>`Offer: ShopOffer`<br>`Price: int`<br>`AddedAmount: int`<br>`ReserveAmmo: int` | После списания цены патронов и успешного увеличения резерва активного оружия | Игрок | Низкая | Средний: баланс и reserve ammo уже изменены; обработчик не должен повторять операцию |

## Правила для обработчиков

1. На событиях с нагрузкой `Высокая` не выполнять запросы к БД/сети, чтение файлов, синхронное ожидание задач и подробное логирование каждого вызова. Скопируйте минимальные примитивные данные в bounded queue и обрабатывайте их отдельно.
2. Не хранить `IPlayer`, entity/projectile и другие игровые объекты для поздней работы без повторной проверки `IsValid`, `SessionId` и принадлежности текущей карте.
3. Не использовать `async void`: dispatcher не сможет дождаться такой работы или изолировать исключение после первого `await`.
4. Не вызывать из обработчика ту же операцию без guard — это создаёт рекурсивный dispatch.
5. Всегда отписываться при выгрузке плагина. Для одного и того же delegate каждый `Unhook` удаляет последнюю соответствующую регистрацию.
6. В `...ing` проверять `IsCancelled`, если обработчик с низким приоритетом должен только наблюдать. Не пытаться «разотменить» контекст: контракт этого не предоставляет.
