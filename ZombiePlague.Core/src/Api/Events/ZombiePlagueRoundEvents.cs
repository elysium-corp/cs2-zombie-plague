using Common.Hooks;
using Common.Hooks.Abstractions;
using ZombiePlague.Api.Events;
using ZombiePlague.Api.Events.Contexts.Round;

namespace ZombiePlague.Core.Api.Events;

internal sealed class ZombiePlagueRoundEvents(IHookSubscriber hooks) : IZombiePlagueRoundEvents
{
    public IHookSubscription<RoundPreparingContext> Preparing { get; } =
        new HookEvent<RoundPreparingContext>(hooks);

    public IHookSubscription<RoundPreparedContext> Prepared { get; } =
        new HookEvent<RoundPreparedContext>(hooks);

    public IHookSubscription<RoundStartingContext> Starting { get; } =
        new HookEvent<RoundStartingContext>(hooks);

    public IHookSubscription<RoundStartedContext> Started { get; } =
        new HookEvent<RoundStartedContext>(hooks);

    public IHookSubscription<RoundStartRejectedContext> StartRejected { get; } =
        new HookEvent<RoundStartRejectedContext>(hooks);

    public IHookSubscription<RoundStartFailedContext> StartFailed { get; } =
        new HookEvent<RoundStartFailedContext>(hooks);

    public IHookSubscription<RoundEndingContext> Ending { get; } =
        new HookEvent<RoundEndingContext>(hooks);

    public IHookSubscription<RoundEndedContext> Ended { get; } =
        new HookEvent<RoundEndedContext>(hooks);

    public IHookSubscription<RoundSchedulingContext> Scheduling { get; } =
        new HookEvent<RoundSchedulingContext>(hooks);

    public IHookSubscription<RoundScheduledContext> Scheduled { get; } =
        new HookEvent<RoundScheduledContext>(hooks);

    public IHookSubscription<RoundScheduleClearingContext> ScheduleClearing { get; } =
        new HookEvent<RoundScheduleClearingContext>(hooks);

    public IHookSubscription<RoundScheduleClearedContext> ScheduleCleared { get; } =
        new HookEvent<RoundScheduleClearedContext>(hooks);
}
