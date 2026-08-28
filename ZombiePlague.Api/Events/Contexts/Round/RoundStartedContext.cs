using Common.Hooks.Abstractions;
using ZombiePlague.Api.Data.Rounds;

namespace ZombiePlague.Api.Events.Contexts.Round;

/// <summary>
/// Контекст успешно запущенного режима раунда.
/// </summary>
public readonly struct RoundStartedContext(IRound round) : IPostHookContext
{
    /// <summary>Запущенный режим раунда.</summary>
    public IRound Round { get; } = round;
}
