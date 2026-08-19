namespace Common.Hooks.Abstractions;

public interface IHookSubscriber
{
    void Hook<TContext>(
        HookHandler<TContext> handler, 
        HookPriority priority = HookPriority.Normal
    ) where TContext : struct, IHookContext;

    void Unhook<TContext>(HookHandler<TContext> handler) where TContext : struct, IHookContext;
}