# Metrics.Core

`Metrics.Core` is a standalone SwiftlyS2 plugin that sends game events to the
ElysiumMetrics ingestion API in Flute CMS. Game plugins depend only on
`Metrics.Api.IMetricsService`; HTTP requests, batching, retry and persistent
storage remain isolated in `Metrics.Core`.

## Runtime behavior

- `Track(...)` only serializes and writes to a bounded in-memory channel;
- a single background worker sends batches to `/api/metrics/v1/events`;
- timeouts, HTTP `408`, `425`, `429`, `5xx` and network errors use exponential retry;
- authorization failures are persisted and retried later;
- validation and other permanent `4xx` failures are logged and discarded;
- undelivered events survive restarts in
  `(swRoot)/data/Metrics.Core/metrics-spool.jsonl`;
- `eventId` is stable across retries, so Flute can safely deduplicate an event
  when a response is lost;
- a full queue never blocks the game thread.

`ZombiePlague.Core` treats Metrics as optional. If `Metrics.Core` is missing or
unavailable, gameplay continues and analytics calls become no-ops.

## Build and installation

```bash
dotnet restore CS2ZombiePlague.sln
dotnet build CS2ZombiePlague.sln -c Release --no-restore
```

Copy both build outputs to the SwiftlyS2 server:

```text
output/Metrics.Core/       -> (swRoot)/plugins/Metrics.Core/
output/ZombiePlague.Core/  -> (swRoot)/plugins/ZombiePlague.Core/
```

Install both folders before restarting the server. This ensures that the
shared `IMetricsService` is available when `ZombiePlague.Core` binds optional
plugin interfaces.

On first load SwiftlyS2 creates:

```text
(swRoot)/configs/plugins/Metrics.Core/metrics.json
```

Configure it and restart `Metrics.Core` and `ZombiePlague.Core` (a full server
restart is the safest first deployment):

```json
{
  "MetricsConfig": {
    "Enabled": true,
    "BaseUrl": "https://elysiumcs.su",
    "ApiSecret": "emx_1_REPLACE_WITH_THE_GENERATED_SECRET",
    "ServerId": 1,
    "ReleaseVersion": "0.1.0",
    "IncludeMap": true,
    "IncludeSessionId": true,
    "BatchSize": 50,
    "QueueCapacity": 5000,
    "FlushIntervalMilliseconds": 2000,
    "RequestTimeoutSeconds": 10,
    "RetryCount": 4,
    "RetryBaseDelayMilliseconds": 500,
    "RetryMaxDelaySeconds": 15,
    "MaxEventBytes": 16384,
    "PersistentSpoolEnabled": true,
    "SpoolFileName": "metrics-spool.jsonl",
    "MaxSpoolBytes": 52428800,
    "SchemaVersions": {
      "class_selected": 1
    }
  }
}
```

Do not commit or publish the real `ApiSecret`.

## First event: `class_selected`

### 1. Enable the game server in Flute

1. Open `Elysium -> Метрики -> Серверы`.
2. Find the required game server and click `Включить Metrics`.
3. Click `Создать Secret` (or rotate the existing one).
4. Copy the secret immediately; Flute shows its full value only once.
5. Put the server card's numeric ID into `ServerId` and the copied value into
   `ApiSecret` in `metrics.json`.

The server ID encoded in `emx_<server_id>_...`, the `ServerId` config value and
the Flute server card ID must be the same.

### 2. Create the event contract

Open `Elysium -> Метрики -> События -> Создать событие` and use:

| Field | Value |
|---|---|
| Name | `Выбран класс` |
| Event key | `class_selected` |
| Category | `Классы` |
| Status | `Активно` |
| Description | `Игрок выбрал класс в меню ZombiePlague.Core` |

Enable this context:

| Context | Enabled | Required |
|---|---:|---:|
| Server ID | yes | yes |
| SteamID игрока | yes | yes |
| Session ID | yes | no |
| Map | yes | no |
| Release version | yes | no |
| Round ID | no | no |

Add three properties:

| Name | Key | Type | Required | Nullable | Suggested constraint |
|---|---|---|---:|---:|---|
| ID класса | `class_id` | String | yes | no | max length `64` |
| Название класса | `class_name` | String | yes | no | max length `128` |
| Тип класса | `class_type` | Enum | yes | no | value `zombie`, label `Зомби` |

Save the event. Its first schema version is `1`, which must match
`SchemaVersions.class_selected` in `metrics.json`.

### 3. Produce and verify the event

Restart the server, join it, open the zombie class menu with `!zclass` and
choose a class that is not already selected. `ZombiePlague.Core` executes:

```csharp
metrics.Track(
    "class_selected",
    player.SteamID,
    new
    {
        class_id = zClass.InternalName,
        class_name = zClass.DisplayName,
        class_type = "zombie"
    }
);
```

Within the configured flush interval, open
`Elysium -> Метрики -> Raw Events`. The event should have status `ACCEPTED`.
If it is `REJECTED`, open the row: Flute displays the exact field and reason.

## Adding another event

1. Create and activate its contract in Flute.
2. Add its current schema version to `SchemaVersions`.
3. Inject `IMetricsService` into the producing service and call `Track` only
   after the game action succeeds.
4. Keep property keys identical to the Flute schema, including letter case.
5. If a production schema change creates version `2`, update the matching
   config entry and deploy the corresponding server code together.
