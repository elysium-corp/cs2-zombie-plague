using Common.Hooks.Abstractions;

namespace ZombiePlague.Api.Events.Contexts.Round;

public struct RoundStartPreContext(string roundId ) : IPreHookContext
{
    public string OriginalRoundId { get; } = roundId;

    public string RoundId { get; set; } = roundId;

    public bool IsCancelled { get; private set; }

    public void Cancel()
    {
        IsCancelled = true;
    }
}