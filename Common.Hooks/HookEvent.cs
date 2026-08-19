using Common.Hooks.Abstractions;

namespace Common.Hooks;

public sealed class HookEvent<TContext>(IHookSubscriber hooks) : IHookEvent<TContext> where TContext : struct, IHookContext
{
    public event HookHandler<TContext> Event
    {
        add => hooks.Hook(value);
        remove => hooks.Unhook(value);
    }

    public void Hook(HookHandler<TContext> handler, HookPriority priority = HookPriority.Normal)
    {
        hooks.Hook(handler, priority);
    }

    public void Unhook(HookHandler<TContext> handler)
    {
        hooks.Unhook(handler);
    }
}