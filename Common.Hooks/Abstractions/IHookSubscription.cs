namespace Common.Hooks.Abstractions;

/// <summary>Публичная точка подписки на один тип hook-контекста.</summary>
/// <typeparam name="TContext">Тип контекста события.</typeparam>
public interface IHookSubscription<TContext> where TContext : struct, IHookContext
{
    /// <summary>Регистрирует обработчик с указанным приоритетом.</summary>
    void Hook(HookHandler<TContext> handler, HookPriority priority = HookPriority.Normal);

    /// <summary>Удаляет последнюю соответствующую регистрацию обработчика.</summary>
    void Unhook(HookHandler<TContext> handler);
}
