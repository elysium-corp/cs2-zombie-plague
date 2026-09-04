# Advertisement.Core

Плагин автоматических рекламных и информационных сообщений Elysium для CS2.

## PostgreSQL и EF Core

Основной источник данных — PostgreSQL. Runtime использует общую инфраструктуру `Common.Database`, `Entity Framework Core 9.0.3` и именованное подключение SwiftlyS2 `elysium_zp_server_1`.

База данных является границей данных сервера: отдельный `ServerId` не используется. Плагин и Flute CMS читают и изменяют одни и те же настройки и сообщения в выбранной PostgreSQL-базе.

PostgreSQL читается один раз при запуске плагина, при фактической смене карты и по ручной команде `ads_reload`. Периодический polling отсутствует; отправка сообщений всегда работает с текущим snapshot в памяти. Поле `refresh_interval_seconds` пока сохраняется в схеме для обратной совместимости, но runtime его больше не использует.

Текст и его цветовая разметка полностью поступают из `Localization.Core`.
Историческая колонка `advertisement.settings.colors` остаётся в схеме для
совместимости, но отсутствует в runtime-конфиге, snapshot и fallback-экспорте.
Динамические значения (`player_name`, `server_name` и другие) передаются в типизированные
`FormatForPlayer` / `FormatForLanguage`: обязательность и тип проверяет
`Localization.Core`, там же значения очищаются от цветовых тегов.

Теги полностью принадлежат `Localization.Core`: метаданные находятся в
`localization.tags`, а переводы — в общем каталоге под ключами `Tag.<TagKey>`.
Advertisement хранит в сообщении только nullable-ссылку `tag_key`, получает
локализованный текст и цвет через `ILocalizationApi` и не содержит собственных
таблиц, конфигурации или runtime-cache с определениями тегов.

Connection string и пароль не хранятся в `advertisement.json`. Для design-time генерации миграций можно временно задать `ADVERTISEMENT_DB_CONNECTION`.

Историческая первая миграция создаёт прежние таблицы тегов, а миграция
`20260904121000_ReferenceLocalizationTags` переносит данные в Localization,
заменяет `tag_id` на `tag_key` и удаляет `advertisement.tags` вместе с
`advertisement.tag_translations`.

Миграция `20260827110000_RemoveAdvertisementServerScope` удаляет устаревшие поля `server_id`. Если в старой БД есть конфликтующие записи с одинаковым `key`, сохраняется общая запись, а при её отсутствии — запись с минимальным `id`.

Миграция `20260827130000_AddAdvertisementDeliveryRules` добавляет режимы отправки, точное время дня, ежедневные окна и аудитории `all` / `admin_group`.

Ручной `001_advertisement.sql` больше не требуется: миграции запускаются через `DatabaseMigrator<AdvertisementDbContext>` / `context.Database.Migrate()`.

## Отказоустойчивость

При недоступности PostgreSQL уже загруженный snapshot сохраняется. До первой успешной загрузки БД используется локальная модель `AdvertisementConfig` как fallback.

Модуль Flute CMS умеет сгенерировать готовый `advertisement.json` из содержимого выбранной БД. В файл входят настройки, сообщения со ссылками `Tag`, расписания и аудитории, но не входят определения тегов, connection string и другие секреты. Fallback-определения тегов экспортируются только модулем ElysiumLocalization.

## Режимы отправки

- `periodic` — отправка с общим или индивидуальным интервалом, при необходимости только внутри ежедневного окна;
- `daily` — отправка в одно или несколько точных времён дня;
- `manual` — сообщение не участвует в scheduler и вызывается другим плагином через API.

Автоматическое сообщение может быть адресовано всем игрокам или группе из `Admin.Core`. Проверка группы выполняется через `Admin.Api` по runtime-состоянию привилегий без SQL-запросов в игровом потоке.

## Публичный API

Проект `Advertisement.Api` публикует `IAdvertisementApi` с ключом `Advertisement.Api.IAdvertisementApi`.

- `GetPlayerLocale(player)` возвращает эффективную локаль игрока;
- `GetText(messageKey, locale)` и `GetText(messageKey, player)` возвращают локализованный текст из текущего snapshot;
- `Send(player, messageKey, tagKey)` отправляет текст игроку и позволяет переопределить тег;
- `SendToAll(messageKey, tagKey)` отправляет текст всем авторизованным игрокам.

API работает только с памятью и не обращается к PostgreSQL во время вызова.

## Команды

- `ads_status` — состояние рекламного snapshot;
- `ads_reload` — немедленная перезагрузка данных из PostgreSQL;
- `ads_test <key> [locale]` — тест конкретного сообщения;
- `!lang`, `!language`, `!язык` — ручной выбор языка игроком.

Административные команды требуют permission `advertisement.admin`.
