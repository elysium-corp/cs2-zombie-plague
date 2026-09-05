# CustomEquipment.Core

Версия `0.6.1` хранит в PostgreSQL огнестрельное оружие, пять встроенных гранат
и лазерную мину. Реализации поведения остаются в C#, а изменяемые игровые
параметры, доступность, модели и порядок управляются через Game CMS. Экономика
полностью вынесена в `Shop.Core`: каталог экипировки не загружает цены и не
выполняет покупки.

Названия оружия и игровых предметов обязаны ссылаться на ключи
`Localization.Core`. Если ключ ещё не опубликован, сервер безопасно показывает
сохранённый `display_name`. Поля `image_url` используются Game CMS для превью и
не загружаются игровым runtime.

## База данных

Плагин использует подключение Swiftly `elysium_zp_server_1` и схему
`custom_equipment`:

- `weapons` — игровые и визуальные параметры оружия;
- `weapon_sounds` — звуковые события и заменяемые штатные sound events;
- `weapon_sound_files` — исторические данные старого редактора; runtime-каталог их больше не загружает;
- `gameplay_items` — параметры `barrier_nade`, `fire_nade`, `frost_nade`,
  `jump_nade`, `shake_nade` и `laser_mine`; специфичные значения поведения
  хранятся в проверяемом JSONB-объекте `settings`.

Исторические столбцы `item_price`/`ammo_price` и таблицы старого магазина пока
сохраняются только для безопасного обновления существующих установок.
`CustomEquipment.Core` их не читает. Миграция `Shop.Core` переносит доступные
старые позиции в отдельную схему `shop`, после чего цены, категории, лимиты,
cooldown и привилегии обслуживаются только новым модулем.

До первой успешной загрузки гранаты и мина используют встроенные значения.
На первом запуске огнестрельное оружие попадёт в runtime-каталог только после
успешной загрузки справочника. Каталоги обновляются при запуске плагина и один
раз при каждой
загрузке карты; периодический polling отсутствует. Неудачная повторная загрузка
не очищает уже активные снимки.

Настройка витрин и правила их runtime-поведения описаны в
[`Shop.Core/README.md`](../Shop.Core/README.md).

## Перезагрузка

Выполните только из серверной консоли:

```text
custom_equipment_reload
```

Команда запускает обновление немедленно. Без неё изменения применятся при
следующей загрузке карты. Новые выдачи используют обновлённый снимок. Уже
выданные экземпляры оружия с ID,
который остался в каталоге, сохраняют старые параметры; удалённый ID перестаёт
проходить проверку использования.

## Звуки

Поддерживаемые `trigger`:

```text
fire
reload
empty
draw
inspect
zoom
silencer_on
silencer_off
```

Плагин воспроизводит `event_name` через Swiftly SoundEvent и передаёт
`weapon_sounds.volume` в `SoundEvent.Volume` (`public.volume`) перед `Emit()`.
Громкость должна быть конечным числом от `0` до `10`; `0` — без звука,
`1` — исходная громкость. Список `.vsnd`, тип, высота и дополнительные свойства
старого редактора для загрузки события больше не требуются. Если заполнено
`replaces_event_name`, сетевое сообщение именно этого штатного события
подавляется для отслеживаемого кастомного оружия, чтобы звуки не накладывались.

Общий скомпилированный ресурс должен быть установлен как:

```text
soundevents/game_sounds_elysium_weapons.vsndevts_c
```

Исходные события готовятся в Source 2 Workshop Tools / ResourceCompiler.
`ElysiumEquipments 1.16.0` хранит только привязку триггера, имя события и
громкость; экспорта и автоматической генерации `.vsndevts` больше нет.
Изменение громкости требует обновления каталога и новой выдачи оружия, без
пересборки VPK. После добавления новых ресурсов нужна смена карты для precache.

В SwiftlyS2 `1.4.6-beta.8` и проверенном `master` (`ce37bdc8`) нет отдельных
GameEvents `clip_in`/`clip_out`: доступен общий `EventWeaponReload`.
Анимационное событие `AE_CL_EJECT_MAG` относится к клиенту и не является
готовой серверной подпиской на извлечение магазина.
`WeaponSound_t` содержит `WEAPON_SOUND_RELOAD`, а `CCSUsrMsg_ReloadEffect`
не содержит отдельной фазы извлечения или вставки магазина. Добавление этих
триггеров требует проверки анимации/сетевых сообщений конкретной модели;
обычная подписка на `weapon_reload` не даёт точных моментов `clip in/out`.

Источники SwiftlyS2: [EventWeaponReload](https://github.com/swiftly-solution/swiftlys2/blob/ce37bdc8f26ce98059eb2cef72bbd479c4504b1e/managed/src/SwiftlyS2.Generated/GameEvents/Interfaces/EventWeaponReload.cs),
[WeaponSound_t](https://github.com/swiftly-solution/swiftlys2/blob/ce37bdc8f26ce98059eb2cef72bbd479c4504b1e/managed/src/SwiftlyS2.Generated/Schemas/Enums/WeaponSound_t.cs),
[SoundEvent.Volume](https://github.com/swiftly-solution/swiftlys2/blob/ce37bdc8f26ce98059eb2cef72bbd479c4504b1e/managed/src/SwiftlyS2.Shared/Modules/Sounds/SoundEvent.cs).


## Миграции для разработки

Design-time connection задаётся переменной `CUSTOM_EQUIPMENT_DB_CONNECTION`.

```bash
dotnet ef migrations add <Name> \
  --project CustomEquipment.Core/CustomEquipment.Core.csproj \
  --configuration Migrations \
  --context CustomEquipmentDbContext
```
