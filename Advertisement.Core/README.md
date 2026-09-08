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

## Custom HUD

Начиная с версии 2.6.0 реклама поддерживает доставку в чат, Custom HUD или оба канала
Настройки и примеры: [CustomHud.Core](../CustomHud.Core/README.md#реклама)

### Настройка через Flute CMS

`Advertisement.Core 2.7.0` принимает канал и оформление из `advertisement.messages`
Миграция `20260907220000_AddHudDelivery` сохраняет существующие сообщения в чате и добавляет:

| Поле / поле fallback | Значение |
| --- | --- |
| `display_type` / `DisplayType` | `chat`, `hud`, `chat_and_hud` |
| `localization_key` / `LocalizationKey` | Ключ текста чата; необязателен в режиме hud |
| `hud_localization_key` / `HudLocalizationKey` | Ключ описания HUD; необязателен у custom-шаблона с выключенным Description |
| `hud_position` / `HudPosition` | `top_left`, `top_center`, `top_right`, `middle_left`, `center`, `middle_right`, `bottom_left`, `bottom_center`, `bottom_right` |
| `hud_duration_seconds` / `HudDurationSeconds` | 0,5–60 секунд, по умолчанию 8 |
| `hud_style` / `HudStyle` | `notice` или `banner` |

Чат использует `{accent}цвет{/accent}` / `{color:red}цвет{/color}`, HUD — ограниченную HTML-разметку CustomHud.Api
Например: `<font color='#85dcb1'><b>Привет, {player_name}!</b></font>`
HTML из изменённого общего ключа удаляется перед отправкой в чат; HTML-сущности не превращаются в управляющие коды чата
Динамические параметры HUD экранируются перед подстановкой, отдельно от параметров чата
Ключи и настройки сохраняются в snapshot и в экспортируемом `advertisement.json`; переводы по-прежнему экспортируются через Localization

Настройки CMS имеют приоритет над старым `hud_delivery.json`
Этот файл используется только для старого fallback без `DisplayType`; чтобы сохранить старое индивидуальное HUD-правило для объявления из БД, перенесите его в CMS
Явный режим hud никогда не выводит в чат, даже при недоступном HUD или переводе; резервный чат сохраняется только для старого fallback без DisplayType
`chat_and_hud` отправляет каждый канал один раз
Выбор канала не меняет расписание, аудиторию, права `ads_test` и ключ публичного Advertisement API

Порядок обновления: сервер и EF-миграция → ресурсы `elysium_messages_v4` у сервера и клиентов → ElysiumAdvertisements 2.7.0 в Flute
До миграции CMS покажет штатное сообщение о недостающей схеме и не будет писать новые поля
После сохранения выполните `ads_reload`, затем `ads_test <key> [locale]` в игре с permission `advertisement.admin`
Проверьте оба перевода, расположение, меню и случай отключённого CustomHud.Core


### Шаблоны баннеров (2.8.0)

Миграция `20260908070000_AddBannerTemplates` создаёт `advertisement.banner_templates`, ссылки на Header/Title и параметры сообщения
Дизайн выбирается во вкладке «Реклама → Баннеры» CMS. Общая Localization остаётся единственным источником переводов
`hud_localization_key` используется как Description; `banner_header_key` и `banner_title_key` зависят от Variant
`banner_parameters` — объект дополнительных строковых значений, которые Localization приводит к объявленному типу
`player_name`, `steam_id`, `map`, `round`, `players`, `max_players`, `bots`, `total_players`, `time`, `next_map`, `server_name` заполняются сервером и не переопределяются статическими значениями
`round` — TotalRoundsPlayed + 1 (0, если game rules ещё нет)

Snapshot загружает только связанные шаблоны и параметры вместе с сообщениями; SQL не выполняется при рендере
Изменение шаблона увеличивает configuration_version. Применение — следующая карта или ads_reload, экспорт fallback становится устаревшим
Fallback встраивает дизайн в `BannerTemplate`, поля в `BannerHeaderKey`/`BannerTitleKey`, значения в `BannerParameters`
Для внешних плагинов доступен `ICustomBannerApi.ShowLocalized`: можно передать собственный дизайн, ключи и параметры без зависимости от Advertisement.Core
Удаление используемого шаблона или ключа Localization запрещено. Откат миграции останавливается, если есть HUD-only сообщения без чатового ключа, чтобы не потерять данные и не отправить HTML в чат

## Уведомления плагинов (3.0.0)

Advertisement.Core также предоставляет IBannerNotificationApi и snapshot правил `advertisement.notification_rules` и виджетов `advertisement.hud_widgets`. 51 событие из десяти плагинов настраивается в **Реклама → Баннеры → Плагины**. Меню и постоянный HUD способностей остаются отдельными интерфейсами; уведомление о перезарядке способности удалено.

[Настройка, API, параметры, миграции и fallback](../docs/banner-notifications.md). Перечень общего контекста распространяется и на рекламу, включая `{round}`, `{roundName}`, `{player}`, состояние игрока и режим ZombiePlague. Параметры конкретного события не становятся автоматическими параметрами объявлений.
