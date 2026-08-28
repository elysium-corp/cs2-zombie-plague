# Common.Hooks

`Common.Hooks` — синхронный типизированный dispatcher для событий между модулями. Публичный API отдаёт только точки подписки `IHookSubscription<TContext>`, а `Core` публикует контексты через `IHookPublisher`.

Полный каталог доменных событий, точки вызова, частота, нагрузка и риски находятся в [`docs/events.md`](../docs/events.md).

## Имена

События сгруппированы по предметной области:

```csharp
zombiePlague.Events.Players.Infecting
zombiePlague.Events.Players.Infected
customEquipment.Events.Items.Purchasing
customEquipment.Events.Items.Purchased
economy.Events.Transactions.Committed
```

Соглашение об именах:

- `...ing` / `...Processing` — операция ещё не выполнена; контекст изменяемый и отменяемый;
- `...ed` / `...Committed` — операция успешно завершила указанную стадию;
- `...Rejected` — ожидаемый отказ без технической ошибки;
- `...Failed` — техническая ошибка, обычно с `Exception` в контексте.

В публичном API нет параллельных `Pre`/`Post`-веток и дублирующих C#-событий с суффиксом `Event`.

## Контекст до операции

```csharp
public struct PlayerInfectingContext(
    IPlayer player,
    IPlayer? infector = null
) : IPreHookContext
{
    public IPlayer Player { get; set; } = player;
    public IPlayer? Infector { get; set; } = infector;

    public bool IsCancelled { get; private set; }

    public void Cancel() => IsCancelled = true;
}
```

Публикация:

```csharp
var context = new PlayerInfectingContext(player, infector);

if (!hooks.DispatchCancellable(ref context))
{
    return false;
}

// Повторно валидируем изменяемые значения и выполняем операцию.
```

`Cancel()` не обрывает цепочку обработчиков: оставшиеся подписчики увидят `IsCancelled == true`. Разотмена не поддерживается.

## Контекст результата

```csharp
public readonly struct PlayerInfectedContext(
    IPlayer player,
    IPlayer? infector = null
) : IPostHookContext
{
    public IPlayer Player { get; } = player;
    public IPlayer? Infector { get; } = infector;
}
```

Результирующие контексты неизменяемы. Они описывают уже произошедший факт и не должны использоваться для попытки отката операции.

## Подписка

```csharp
private void Register()
{
    api.Events.Players.Infecting.Hook(OnInfecting, HookPriority.High);
    api.Events.Players.Infected.Hook(OnInfected);
}

private void Unregister()
{
    api.Events.Players.Infecting.Unhook(OnInfecting);
    api.Events.Players.Infected.Unhook(OnInfected);
}

private void OnInfecting(ref PlayerInfectingContext context)
{
    if (ShouldBlock(context.Player))
    {
        context.Cancel();
    }
}

private void OnInfected(ref PlayerInfectedContext context)
{
    // Короткая синхронная реакция.
}
```

Порядок вызова: `High`, `Normal`, `Low`; внутри одного приоритета — порядок подписки.

## Потоки, исключения и стоимость

- `Dispatch` выполняется в потоке вызывающего `Core`; автоматического перехода в scheduler или thread pool нет.
- Исключение подписчика передаётся в optional exception handler `HookService` и не прерывает остальных подписчиков.
- Если exception handler не задан, исключение подписчика подавляется. Production-модулям рекомендуется подключать логирование с rate limit.
- Хранилище обработчиков использует copy-on-write. Подписка/отписка создаёт новый snapshot, а горячий `Dispatch` только берёт готовый массив и последовательно вызывает handlers.
- При отсутствии подписчиков выполняются короткая блокировка и lookup по типу контекста.
- Стоимость одного dispatch линейна по числу подписчиков: `O(N)`. Самая большая опасность — не dispatcher, а блокирующая или тяжёлая работа внутри handler.

## Ограничения API

- Контекст должен быть `struct` и реализовывать `IHookContext`.
- Handler имеет сигнатуру `void Handler(ref TContext context)`; `async void` использовать нельзя.
- Изменяемые значения pre-контекста должны повторно валидироваться поставщиком события.
- Каждый публичный event contract должен иметь реальную точку `Dispatch` в соответствующем `Core`.
- Подписчик обязан выполнить симметричный `Unhook` до уничтожения своего DI-контейнера.
