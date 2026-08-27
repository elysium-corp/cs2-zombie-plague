using Common.Hooks.Abstractions;

namespace Common.Hooks;

/// <summary>
/// Методы публикации hook-контекстов.
/// </summary>
public static class HookPublisherExtensions
{
    /// <summary>
    /// Публикует отменяемый pre-hook и возвращает <see langword="false"/>,
    /// если хотя бы один обработчик отменил операцию.
    /// Все зарегистрированные обработчики вызываются независимо от отмены.
    /// </summary>
    public static bool DispatchCancellable<TContext>(this IHookPublisher publisher, ref TContext context)
        where TContext : struct, IPreHookContext
    {
        ArgumentNullException.ThrowIfNull(publisher);

        publisher.Dispatch(ref context);
        return !context.IsCancelled;
    }
}
