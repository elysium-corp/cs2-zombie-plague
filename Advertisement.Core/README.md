# Advertisement.Core

Плагин автоматических рекламных и информационных сообщений Elysium для CS2.

## PostgreSQL и EF Core

Основной источник данных — PostgreSQL. Runtime использует общую инфраструктуру `Common.Database`, `Entity Framework Core 9.0.3` и именованное подключение SwiftlyS2 `elysium_zp_server_1`.

База данных является границей данных сервера: отдельный `ServerId` не используется. Плагин и Flute CMS читают и изменяют одни и те же настройки, теги и сообщения в выбранной PostgreSQL-базе.

Connection string и пароль не хранятся в `advertisement.json`. Для design-time генерации миграций можно временно задать `ADVERTISEMENT_DB_CONNECTION`.

Первая миграция создаёт схемы/таблицы `advertisement.settings`, `advertisement.tags`, `advertisement.tag_translations`, `advertisement.messages`, `advertisement.message_translations` и поле локали в `core.player_preferences`.

Миграция `20260827110000_RemoveAdvertisementServerScope` удаляет устаревшие поля `server_id`. Если в старой БД есть конфликтующие записи с одинаковым `key`, сохраняется общая запись, а при её отсутствии — запись с минимальным `id`.

Ручной `001_advertisement.sql` больше не требуется: миграции запускаются через `DatabaseMigrator<AdvertisementDbContext>` / `context.Database.Migrate()`.

## Отказоустойчивость

При недоступности PostgreSQL уже загруженный snapshot сохраняется. До первой успешной загрузки БД используется локальная модель `AdvertisementConfig` как fallback.

## Команды

- `ads_status` — состояние рекламного snapshot;
- `ads_reload` — немедленная перезагрузка данных из PostgreSQL;
- `ads_test <key> [locale]` — тест конкретного сообщения;
- `!lang`, `!language`, `!язык` — ручной выбор языка игроком.

Административные команды требуют permission `advertisement.admin`.
