using Common.Hooks;
using Common.Hooks.Abstractions;
using Economy.Api.Events;

namespace Economy.Core.Api;

internal sealed class EconomyEvents(
    EconomyTransactionEvents transactions,
    EconomyAccountEvents accounts
) : IEconomyEvents
{
    public IEconomyTransactionEvents Transactions => transactions;

    public IEconomyAccountEvents Accounts => accounts;
}

internal sealed class EconomyTransactionEvents(IHookSubscriber hooks) : IEconomyTransactionEvents
{
    public IHookSubscription<EconomyTransactionProcessingContext> Processing { get; } =
        new HookEvent<EconomyTransactionProcessingContext>(hooks);

    public IHookSubscription<EconomyTransactionCommittedContext> Committed { get; } =
        new HookEvent<EconomyTransactionCommittedContext>(hooks);

    public IHookSubscription<EconomyTransactionRejectedContext> Rejected { get; } =
        new HookEvent<EconomyTransactionRejectedContext>(hooks);

    public IHookSubscription<EconomyTransactionFailedContext> Failed { get; } =
        new HookEvent<EconomyTransactionFailedContext>(hooks);
}

internal sealed class EconomyAccountEvents(IHookSubscriber hooks) : IEconomyAccountEvents
{
    public IHookSubscription<EconomyAccountInitializedContext> Initialized { get; } =
        new HookEvent<EconomyAccountInitializedContext>(hooks);

    public IHookSubscription<EconomyAccountLoadedContext> Loaded { get; } =
        new HookEvent<EconomyAccountLoadedContext>(hooks);

    public IHookSubscription<EconomyAccountLoadFailedContext> LoadFailed { get; } =
        new HookEvent<EconomyAccountLoadFailedContext>(hooks);

    public IHookSubscription<EconomyAccountRemovedContext> Removed { get; } =
        new HookEvent<EconomyAccountRemovedContext>(hooks);

    public IHookSubscription<EconomyAccountSavedContext> Saved { get; } =
        new HookEvent<EconomyAccountSavedContext>(hooks);

    public IHookSubscription<EconomyAccountSaveFailedContext> SaveFailed { get; } =
        new HookEvent<EconomyAccountSaveFailedContext>(hooks);
}
