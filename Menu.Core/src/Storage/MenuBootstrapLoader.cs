using System.Collections.Immutable;
using Menu.Api.Contracts;
using Menu.Api.Results;
using Menu.Core.Runtime;
using Menu.Core.Validation;

namespace Menu.Core.Storage;

internal sealed record MenuBootstrapAttempt(
    MenuSnapshotSource Source,
    bool Activated,
    ImmutableArray<MenuValidationIssue> Issues);

internal sealed record MenuBootstrapResult(
    bool Activated,
    MenuSnapshotSource Source,
    MenuRuntimeSnapshot Snapshot,
    ImmutableArray<MenuBootstrapAttempt> Attempts);

internal sealed class MenuBootstrapLoader
{
    private readonly MenuReleaseFileStore _fileStore;
    private readonly MenuSnapshotStore _snapshotStore;

    public MenuBootstrapLoader(MenuReleaseFileStore fileStore, MenuSnapshotStore snapshotStore)
    {
        _fileStore = fileStore ?? throw new ArgumentNullException(nameof(fileStore));
        _snapshotStore = snapshotStore ?? throw new ArgumentNullException(nameof(snapshotStore));
    }

    public async ValueTask<MenuBootstrapResult> TryActivateLocalAsync(
        string lastKnownGoodPath,
        string fallbackPath,
        MenuReleaseValidationContext context,
        DateTimeOffset attemptedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await TryActivateLocalAsync(
            lastKnownGoodPath,
            fallbackPath,
            _ => context,
            attemptedAt,
            cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask<MenuBootstrapResult> TryActivateLocalAsync(
        string lastKnownGoodPath,
        string fallbackPath,
        Func<MenuReleaseDefinition, MenuReleaseValidationContext> contextFactory,
        DateTimeOffset attemptedAt,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contextFactory);
        var attempts = ImmutableArray.CreateBuilder<MenuBootstrapAttempt>(2);

        var lkg = await _fileStore.LoadAsync(
            lastKnownGoodPath,
            MenuSnapshotSource.LastKnownGood,
            contextFactory,
            cancellationToken).ConfigureAwait(false);
        var lkgActivation = lkg.Context is null ? null : TryActivate(lkg, lkg.Context, attemptedAt);
        if (!lkg.IsValid)
        {
            _snapshotStore.RecordRejected(lkg.Source, attemptedAt, lkg.Validation);
        }

        attempts.Add(new MenuBootstrapAttempt(
            MenuSnapshotSource.LastKnownGood,
            lkgActivation?.Activated == true,
            lkgActivation?.Validation.Issues ?? lkg.Validation.Issues));
        if (lkgActivation?.Activated == true)
        {
            return new MenuBootstrapResult(
                true,
                MenuSnapshotSource.LastKnownGood,
                lkgActivation.Snapshot,
                attempts.ToImmutable());
        }

        var fallback = await _fileStore.LoadAsync(
            fallbackPath,
            MenuSnapshotSource.Fallback,
            contextFactory,
            cancellationToken).ConfigureAwait(false);
        var fallbackActivation = fallback.Context is null ? null : TryActivate(fallback, fallback.Context, attemptedAt);
        if (!fallback.IsValid)
        {
            _snapshotStore.RecordRejected(fallback.Source, attemptedAt, fallback.Validation);
        }

        attempts.Add(new MenuBootstrapAttempt(
            MenuSnapshotSource.Fallback,
            fallbackActivation?.Activated == true,
            fallbackActivation?.Validation.Issues ?? fallback.Validation.Issues));
        if (fallbackActivation?.Activated == true)
        {
            return new MenuBootstrapResult(
                true,
                MenuSnapshotSource.Fallback,
                fallbackActivation.Snapshot,
                attempts.ToImmutable());
        }

        return new MenuBootstrapResult(
            false,
            MenuSnapshotSource.None,
            _snapshotStore.Current,
            attempts.ToImmutable());
    }

    private MenuSnapshotActivationResult? TryActivate(
        MenuFileLoadResult file,
        MenuReleaseValidationContext context,
        DateTimeOffset attemptedAt)
    {
        return file.IsValid && file.Release is not null
            ? _snapshotStore.TryActivate(file.Release, context, file.Source, attemptedAt)
            : null;
    }
}
