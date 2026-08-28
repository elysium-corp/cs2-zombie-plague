using Common.Hooks.Abstractions;

namespace Common.Hooks;

/// <summary>
/// Потокобезопасный синхронный dispatcher контекстов с приоритетами и snapshot-семантикой.
/// </summary>
/// <param name="exceptionHandler">Необязательный обработчик исключений подписчиков.</param>
public sealed class HookService(Action<Exception, Type, Delegate>? exceptionHandler = null) : IHookSubscriber, IHookPublisher
{
    private readonly Lock _sync = new();

    // Массивы никогда не изменяются после публикации в словаре. Dispatch получает
    // стабильный snapshot без выделения памяти на каждом игровом событии.
    private readonly Dictionary<Type, HookRegistration[]> _hooks = [];

    private long _registrationOrder;

    /// <inheritdoc />
    public void Hook<TContext>(
        HookHandler<TContext> handler,
        HookPriority priority = HookPriority.Normal)
        where TContext : struct, IHookContext
    {
        ArgumentNullException.ThrowIfNull(handler);

        var contextType = typeof(TContext);

        var registration = new HookRegistration(
            handler,
            priority,
            Interlocked.Increment(ref _registrationOrder)
        );

        lock (_sync)
        {
            var registrations = _hooks.GetValueOrDefault(contextType) ?? [];
            var updated = new HookRegistration[registrations.Length + 1];

            registrations.CopyTo(updated, 0);
            updated[^1] = registration;

            Array.Sort(updated, static (left, right) =>
            {
                var priorityComparison = right.Priority.CompareTo(left.Priority);

                if (priorityComparison != 0)
                {
                    return priorityComparison;
                }

                return left.Order.CompareTo(right.Order);
            });

            _hooks[contextType] = updated;
        }
    }

    /// <inheritdoc />
    public void Unhook<TContext>(HookHandler<TContext> handler) where TContext : struct, IHookContext
    {
        ArgumentNullException.ThrowIfNull(handler);

        var contextType = typeof(TContext);

        lock (_sync)
        {
            if (!_hooks.TryGetValue(contextType, out var registrations))
            {
                return;
            }

            var removeIndex = -1;
            var latestOrder = long.MinValue;

            for (var index = 0; index < registrations.Length; index++)
            {
                var registration = registrations[index];

                if (Equals(registration.Handler, handler) && registration.Order > latestOrder)
                {
                    removeIndex = index;
                    latestOrder = registration.Order;
                }
            }

            if (removeIndex < 0)
            {
                return;
            }

            if (registrations.Length == 1)
            {
                _hooks.Remove(contextType);
                return;
            }

            var updated = new HookRegistration[registrations.Length - 1];

            Array.Copy(registrations, 0, updated, 0, removeIndex);
            Array.Copy(
                registrations,
                removeIndex + 1,
                updated,
                removeIndex,
                registrations.Length - removeIndex - 1
            );

            _hooks[contextType] = updated;
        }
    }

    /// <inheritdoc />
    public void Dispatch<TContext>(ref TContext context) where TContext : struct, IHookContext
    {
        var contextType = typeof(TContext);

        HookRegistration[] registrations;

        lock (_sync)
        {
            if (!_hooks.TryGetValue(contextType, out registrations))
            {
                return;
            }
        }

        foreach (var registration in registrations)
        {
            var handler = (HookHandler<TContext>)registration.Handler;

            try
            {
                handler(ref context);
            }
            catch (Exception exception)
            {
                exceptionHandler?.Invoke(exception, contextType, registration.Handler);
            }
        }
    }

    private sealed record HookRegistration(
        Delegate Handler,
        HookPriority Priority,
        long Order
    );
}
