namespace Common.Hooks.Abstractions;

/// <summary>
/// Контекст события, вызываемого перед выполнением операции.
/// Позволяет отменить операцию через <see cref="Cancel"/>.
/// </summary>
public interface IPreHookContext : IHookContext
{
    /// <summary>
    /// Указывает, была ли операция отменена одним из обработчиков.
    /// </summary>
    bool IsCancelled { get; }

    /// <summary>
    /// Помечает операцию как отменённую.
    /// Не останавливает выполнение остальных hook-обработчиков.
    /// </summary>
    void Cancel();
}