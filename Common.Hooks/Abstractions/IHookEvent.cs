namespace Common.Hooks.Abstractions;

public interface IHookEvent<TContext> :
    IEventSubscription<TContext>,
    IHookSubscription<TContext>
    where TContext : struct, IHookContext;