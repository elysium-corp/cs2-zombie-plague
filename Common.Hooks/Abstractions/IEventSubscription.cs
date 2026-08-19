namespace Common.Hooks.Abstractions;

public interface IEventSubscription<TContext> where TContext : struct, IHookContext
{
    event HookHandler<TContext> Event;
}