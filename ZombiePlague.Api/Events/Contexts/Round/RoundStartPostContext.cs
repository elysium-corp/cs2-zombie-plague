using Common.Hooks.Abstractions;
using ZombiePlague.Api.Data.Rounds;

namespace ZombiePlague.Api.Events.Contexts.Round;

public readonly struct RoundStartPostContext(IRound round) : IPostHookContext
{
    public IRound Round { get; } = round;
}