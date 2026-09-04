# Advertisement.Core

Плагин автоматических рекламных и информационных сообщений Elysium для CS2.

## PostgreSQL и EF Core

Основной источник данных — PostgreSQL. Runtime использует общую инфраструктуру `Common.Database`, `Entity Framework Core 9.0.3` и именованное подключение SwiftlyS2 `elysium_zp_server_1`.

База данных является границей данных сервера: отдельный `ServerId` не используется. Плагин и Flute CMS читают и изменяют одни и те же настройки, теги и сообщения в выбранной PostgreSQL-базе.

PostgreSQL читается один раз при запуске плагина, при фактической смене карты и по ручной команде `ads_reload`. Периодический polling отсутствует; отправка сообщений всегда работает с текущим snapshot в памяти. Поле `refresh_interval_seconds` пока сохраняется в схеме для обратной совместимости, но runtime его больше не использует.

Текст и его цветовая разметка полностью поступают из `Localization.Core`.
Палитра `advertisement.settings.colors` сохраняется только для совместимости со
старыми конфигами и больше не обрабатывает сообщения повторно. Динамические
значения (`player_name`, `server_name` и другие) передаются в типизированные
`FormatForPlayer` / `FormatForLanguage`: обязательность и тип проверяет
`Localization.Core`, там же значения очищаются от цветовых тегов.

Названия тегов принадлежат только группе Tags и хранятся в
`advertisement.tag_translations`. Ключи вида `advertisement.tags.*` в общем
каталоге Localization не создаются и при обновлении удаляются после безопасного
переноса существующих переводов в таблицу тегов.

Connection string и пароль не хранятся в `advertisement.json`. Для design-time генерации миграций можно временно задать `ADVERTISEMENT_DB_CONNECTION`.

Первая миграция создаёт схемы/таблицы `advertisement.settings`, `advertisement.tags`, `advertisement.tag_translations`, `advertisement.messages`, `advertisement.message_translations` и поле локали в `core.player_preferences`.

Миграция `20260827110000_RemoveAdvertisementServerScope` удаляет устаревшие поля `server_id`. Если в старой БД есть конфликтующие записи с одинаковым `key`, сохраняется общая запись, а при её отсутствии — запись с минимальным `id`.

Миграция `20260827130000_AddAdvertisementDeliveryRules` добавляет режимы отправки, точное время дня, ежедневные окна и аудитории `all` / `admin_group`.

Ручной `001_advertisement.sql` больше не требуется: миграции запускаются через `DatabaseMigrator<AdvertisementDbContext>` / `context.Database.Migrate()`.

## Отказоустойчивость

При недоступности PostgreSQL уже загруженный snapshot сохраняется. До первой успешной загрузки БД используется локальная модель `AdvertisementConfig` как fallback.

Модуль Flute CMS умеет сгенерировать готовый `advertisement.json` из содержимого выбранной БД. В файл входят настройки, теги, переводы, расписания и аудитории, но не входят connection string и другие секреты.

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
