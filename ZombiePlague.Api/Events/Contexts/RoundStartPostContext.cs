using Common.Hooks.Abstractions;
using ZombiePlague.Api.Data;

namespace ZombiePlague.Api.Events.Contexts;

public struct RoundStartPostContext(IRound round) : IPostHookContext
{
    public IRound Round { get; set; } = round;
}