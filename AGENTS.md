# AGENTS.md

## Project overview

Zombie Plague is a modular Counter-Strike 2 server modification built with C#,
.NET 10 and SwiftlyS2. The solution consists of independent plugins, public API
contracts and shared infrastructure.

## Build and validation

Run these commands from the repository root:

```bash
dotnet restore CS2ZombiePlague.sln
dotnet build CS2ZombiePlague.sln --configuration Release --no-restore
```

Core plugin outputs are written to `output/<Module.Core>/`. Do not commit files
from `bin/`, `obj/` or `output/`.

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
