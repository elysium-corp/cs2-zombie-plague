namespace Common.Hooks.Abstractions;

public interface IHookSubscription<TContext> where TContext : struct, IHookContext
{
    void Hook(HookHandler<TContext> handler, HookPriority priority = HookPriority.Normal);

    void Unhook(HookHandler<TContext> handler);
}