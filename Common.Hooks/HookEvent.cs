using Common.Hooks.Abstractions;

namespace Common.Hooks;

/// <summary>
/// Представление одного типа контекста как публичной точки подписки.
/// </summary>
public sealed class HookEvent<TContext>(IHookSubscriber hooks) : IHookSubscription<TContext>
    where TContext : struct, IHookContext
{
    /// <inheritdoc />
    public void Hook(HookHandler<TContext> handler, HookPriority priority = HookPriority.Normal)
    {
        hooks.Hook(handler, priority);
    }

    /// <inheritdoc />
    public void Unhook(HookHandler<TContext> handler)
    {
        hooks.Unhook(handler);
    }
}
