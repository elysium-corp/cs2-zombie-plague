using Common.Hooks.Abstractions;

namespace Common.Hooks;

public sealed class HookService : IHookSubscriber, IHookPublisher
{
    private readonly Lock _sync = new();

    private readonly Dictionary<Type, List<HookRegistration>> _hooks = [];

    private long _registrationOrder;

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
            if (!_hooks.TryGetValue(contextType, out var registrations))
            {
                registrations = [];
                _hooks[contextType] = registrations;
            }

            registrations.Add(registration);

            registrations.Sort(static (left, right) =>
            {
                var priorityComparison = right.Priority.CompareTo(left.Priority);

                if (priorityComparison != 0)
                {
                    return priorityComparison;
                }

                return left.Order.CompareTo(right.Order);
            });
        }
    }

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

            var index = registrations.FindLastIndex(registration =>
                Equals(registration.Handler, handler)
            );

            if (index < 0)
            {
                return;
            }

            registrations.RemoveAt(index);

            if (registrations.Count == 0)
            {
                _hooks.Remove(contextType);
            }
        }
    }

    public void Dispatch<TContext>(ref TContext context) where TContext : struct, IHookContext
    {
        var contextType = typeof(TContext);

        HookRegistration[] registrations;

        lock (_sync)
        {
            if (!_hooks.TryGetValue(contextType, out var registeredHooks))
            {
                return;
            }

            registrations = [.. registeredHooks];
        }

        foreach (var registration in registrations)
        {
            var handler = (HookHandler<TContext>)registration.Handler;

            handler(ref context);
        }
    }

    private sealed record HookRegistration(
        Delegate Handler,
        HookPriority Priority,
        long Order
    );
}