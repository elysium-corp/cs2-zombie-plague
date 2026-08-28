namespace Common.Hooks.Abstractions;

/// <summary>Регистрирует и удаляет синхронные обработчики hook-контекстов.</summary>
public interface IHookSubscriber
{
    /// <summary>Регистрирует обработчик с указанным приоритетом.</summary>
    void Hook<TContext>(
        HookHandler<TContext> handler, 
        HookPriority priority = HookPriority.Normal
    ) where TContext : struct, IHookContext;

    /// <summary>Удаляет последнюю соответствующую регистрацию обработчика.</summary>
    void Unhook<TContext>(HookHandler<TContext> handler) where TContext : struct, IHookContext;
}
