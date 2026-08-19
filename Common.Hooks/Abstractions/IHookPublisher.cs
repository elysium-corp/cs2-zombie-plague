namespace Common.Hooks.Abstractions;

public interface IHookPublisher
{
    /// <summary>
    /// Синхронно передаёт контекст всем зарегистрированным обработчикам.
    /// Изменения контекста сохраняются благодаря передаче через ref.
    /// </summary>
    void Dispatch<TContext>(ref TContext context) where TContext : struct, IHookContext;
}