# AGENTS.md

## Project overview

Zombie Plague is a modular Counter-Strike 2 server modification built with C#,
.NET 10 and SwiftlyS2. The solution consists of independent plugins, public API
contracts and shared infrastructure.

## Build and validation

Run these commands from the repository root:

```bash
pwsh ./scripts/build-package.ps1 -Configuration Release
dotnet test CS2ZombiePlague.sln --configuration Release --no-build --no-restore
```

Server-ready plugin folders are written to `dist/Release/plugins/`; archives and
the complete runtime ZIP are written to `dist/Release/packages/`. Treat
`artifacts/` as compiler output only. Do not commit files from `bin/`, `obj/`,
`artifacts/`, `dist/` or the legacy `output/` directory.

## Architecture conventions

- `*.Api` projects contain public interfaces, events and DTOs. They must not
  depend on `*.Core` projects or implementation details.
- `*.Core` projects contain plugin entry points, DI registrations, services,
  configuration and game-server integrations.
- A Core plugin consumes another plugin only through its public shared API.
- `Common.*` contains reusable infrastructure without feature-specific business
  dependencies.
- Preserve nullable annotations and backward compatibility of public contracts
  unless a breaking change is explicitly requested.
- Новые XML-комментарии и поясняющие комментарии в коде пишутся на русском
  языке.
- Каждый публичный интерфейс и каждый его член должны иметь XML-документацию,
  описывающую назначение и контракт использования.

## Обязательная локализация

- Единственная система переводов проекта — `Localization.Core` и её публичный
  `Localization.Api.ILocalizationApi`. Это обязательное правило для всех новых
  модулей и новых или изменяемых сообщений существующих модулей.
- Все тексты для игроков и администраторов (HUD, меню, чат, ошибки, подсказки и
  ответы команд) получать через `ILocalizationApi`. Потребитель подключает
  `Localization.Api` и обязательный shared interface; прямые зависимости от
  `Localization.Core` запрещены. HUD-рендерер получает уже локализованный текст.
- Не использовать локализатор SwiftlyS2, `resources/translations`, собственные
  словари переводов, встроенные ru/en fallback-строки или тексты интерфейса прямо
  в runtime-коде, XML и JS. SwiftlyS2 остаётся платформой сервера и средством вывода,
  но не источником переводов. Технические логи, идентификаторы, названия карт из
  каталога и машинный JSON статуса не являются переводами интерфейса.
- Ключи, переводы и схемы параметров добавлять в `Localization.Core`: отдельной
  EF-миграцией и в `resources/templates/template.jsonc`. Уже применённые миграции
  не редактировать, пользовательские переводы не перезаписывать. При изменении
  шаблона обновлять версию, дату и checksum через `FallbackConfigChecksum.Compute`.
- Язык игрока и резервный язык определяет только Localization. Для серверной
  консоли использовать `ServerFallbackLanguage`; не задавать `ru`/`en` в потребителе.
  Параметры форматировать через API, префиксы брать через API тегов. При отсутствии
  ключа допустим возврат самого ключа через `*OrKey`, без локального словаря.
- При добавлении или изменении локализации проверять полноту ключей, параметры,
  резервный язык, валидность общего fallback и сохранность пользовательских
  переводов при миграции. Обход этих требований считать дефектом при review.

## Custom HUD resources

- Для новых Custom HUD Elysium использовать обозначение ресурсов `_v4` и
  проверенную структуру существующих HUD способностей и сообщений
  Суффикс обозначает ревизию ресурсов проекта, а не параметр версии компилятора Valve
- Имена в серверных путях, XML include, генераторе и тестовых fixtures должны совпадать
- Корневая панель XML не имеет `id`; адресуемые сервером панели находятся внутри неё
- Успешные .NET-тесты не заменяют компиляцию XML/CSS актуальными CS2 Workshop Tools

## Lifecycle conventions

`OnLoad` performs early setup, then the module builds its isolated DI container.
`OnStart` initializes local services. Shared APIs are published and consumed
before `OnReady` subscribes to external events and starts gameplay logic.
`OnUnload` must stop background work and unsubscribe hooks while dependencies are
still available. The DI container is then destroyed and `OnStop` performs final
cleanup without resolving disposed services.

## Code Review Rules

Write review comments in Russian. Report only concrete, actionable defects with
meaningful correctness, reliability, security or compatibility impact. Do not
report formatting, naming preferences or speculative improvements. If the
change is safe, return no finding. Every finding must state the failing runtime
condition, its impact and the smallest reasonable correction direction.

### Contracts and module boundaries

Flag a change only when it breaks a public API/DTO/event/nullability contract
without migrating consumers, introduces an `Api -> Core` dependency, accesses
another Core plugin's internals, or creates a dependency cycle.

### Plugin lifecycle and runtime safety

Flag subscriptions without symmetric cleanup, DI access outside the container
lifetime, callbacks/timers/tasks that can run after unload, duplicate handlers
on reload, or unsafe shared state. For gameplay changes, identify the concrete
player, round or infection state that becomes stale or invalid.

### Build, resources and live-server impact

Flag changes that break the .NET 10 solution build, omit required configuration,
gamedata, templates or translations from plugin output, depend on incorrect
path casing on Linux, or introduce unbounded per-tick work that can degrade the
live CS2 server.
