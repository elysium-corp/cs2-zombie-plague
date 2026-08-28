using Common.Hooks.Abstractions;

namespace ZombiePlague.Api.Events.Contexts.Round;

/// <summary>
/// Контекст запуска режима раунда до изменения состояния игры.
/// </summary>
public struct RoundStartingContext(string roundId) : IPreHookContext
{
    /// <summary>Изначально выбранный идентификатор режима.</summary>
    public string OriginalRoundId { get; } = roundId;

    /// <summary>Идентификатор запускаемого режима. Может быть заменён обработчиком.</summary>
    public string RoundId { get; set; } = roundId;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel()
    {
        IsCancelled = true;
    }
}
