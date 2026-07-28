# AGENTS.md

## Project overview

Zombie Plague is a modular Counter-Strike 2 server modification built with C#,
.NET 10 and SwiftlyS2. The solution consists of independent plugins, public API
contracts and shared infrastructure.

## Build and validation

Use the following commands from the repository root:

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
- A Core plugin may consume another plugin only through its public shared API.
  Do not introduce dependencies on another Core plugin's internal services.
- `Common.*` projects contain reusable infrastructure and must not acquire
  feature-specific business dependencies.
- Preserve nullable annotations and backward compatibility of public API
  contracts unless a breaking change is explicitly requested.

## SwiftlyS2 lifecycle

The expected lifecycle is:

1. `OnLoad` performs early setup without resolving DI services.
2. The module builds its isolated `ServiceProvider`.
3. `OnStart` initializes services that only need local dependencies.
4. Shared APIs are published and consumed.
5. `OnReady` subscribes to external events and starts game logic.
6. `OnUnload` unsubscribes hooks and events while dependencies are available.
7. The module destroys and disposes its DI container.
8. `OnStop` performs final cleanup without resolving disposed services.

Every subscription must have a matching unsubscription. Timers, callbacks and
background work must not access plugin state after unload. Disposable services
and the module `ServiceProvider` must be released exactly once.

## Code Review Rules

Write review comments in Russian. Keep reviews concise and report only concrete,
actionable defects with meaningful correctness, reliability, security or
compatibility impact. Do not report formatting preferences, naming nits or
speculative improvements as defects. If the change is safe, do not invent a
finding.

### Public contracts and module boundaries

- Flag breaking changes to public interfaces, DTOs, events, nullability or
  shared-interface behavior when consumers are not migrated in the same change.
- Flag `Api -> Core` dependencies and access to another Core plugin's internal
  implementation.
- Check that new project references preserve the intended dependency direction
  and do not introduce circular dependencies.

### Lifecycle and resources

- Flag event or hook registrations without symmetric cleanup.
- Flag DI access before module creation or after container destruction.
- Flag callbacks, timers or asynchronous work that can execute after unload or
  use disposed services.
- Check reload behavior for duplicate handlers, leaked state and cleanup that is
  not idempotent.

### Gameplay correctness

- Check player validity and connection state before accessing player entities.
- Check round transitions, infection state and delayed callbacks for races or
  stale state.
- Treat shared mutable collections, singleton state and static state as
  potentially concurrent; flag unsafe mutation when callbacks may overlap.
- Flag unbounded loops, timers, allocations or per-tick work that can degrade a
  live game server.

### Configuration and packaging

- Check JSON configuration, gamedata, templates and translations against their
  C# models, required fields and default behavior.
- Ensure required resources are copied into Core plugin output and path casing
  works on Linux.
- Flag changes that compile locally but omit files required by the packaged
  plugin.

### Verification expectations

- Ensure the entire `CS2ZombiePlague.sln` remains buildable on .NET 10.
- For behavior changes, expect focused tests when the code is testable without a
  live CS2 server, or request a concrete manual verification scenario.
- Each finding must explain the failing condition, its impact and the smallest
  reasonable correction direction.
