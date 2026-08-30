using System.Collections.Immutable;
using Menu.Api.Contracts;
using Menu.Api.Enums;
using Menu.Api.Results;
using Menu.Core.Validation;

namespace Menu.Core.Runtime;

internal sealed record MenuSnapshotStatus(
    long ActiveReleaseId,
    MenuSnapshotSource ActiveSource,
    DateTimeOffset LastAttemptAt,
    bool LastAttemptSucceeded,
    ImmutableArray<MenuDiagnostic> LastAttemptDiagnostics);

internal sealed record MenuSnapshotActivationResult(
    bool Activated,
    MenuRuntimeSnapshot Snapshot,
    MenuReleaseValidationResult Validation);

internal sealed class MenuSnapshotStore
{
    private readonly MenuReleaseValidator _validator;
    private readonly MenuSnapshotCompiler _compiler;
    private MenuRuntimeSnapshot _current = MenuRuntimeSnapshot.Empty;
    private MenuSnapshotStatus _status = new(
        0,
        MenuSnapshotSource.None,
        default,
        LastAttemptSucceeded: false,
        LastAttemptDiagnostics: []);

    public MenuSnapshotStore(MenuReleaseValidator validator, MenuSnapshotCompiler compiler)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
    }

    public MenuRuntimeSnapshot Current => Volatile.Read(ref _current);

    public MenuSnapshotStatus Status => Volatile.Read(ref _status);

    public MenuSnapshotActivationResult TryActivate(
        MenuReleaseDefinition? release,
        MenuReleaseValidationContext context,
        MenuSnapshotSource source,
        DateTimeOffset attemptedAt)
    {
        ArgumentNullException.ThrowIfNull(context);
        var validation = _validator.Validate(release, context);
        if (!validation.IsValid || release is null)
        {
            RecordFailure(source, attemptedAt, validation.Issues);
            return new MenuSnapshotActivationResult(false, Current, validation);
        }

        try
        {
            var candidate = _compiler.Compile(release, context, source, attemptedAt, validation);
            Interlocked.Exchange(ref _current, candidate);
            var diagnostics = candidate.Diagnostics;
            Volatile.Write(
                ref _status,
                new MenuSnapshotStatus(
                    candidate.ReleaseId,
                    candidate.Source,
                    attemptedAt,
                    LastAttemptSucceeded: true,
                    diagnostics));
            return new MenuSnapshotActivationResult(true, candidate, validation);
        }
        catch (Exception exception)
        {
            var issues = validation.Issues.Add(new MenuValidationIssue
            {
                Severity = MenuValidationSeverity.Error,
                Code = "snapshot.compile_failed",
                Message = $"Snapshot compilation failed: {exception.GetType().Name}.",
                Path = "$"
            });
            var failedValidation = new MenuReleaseValidationResult(issues);
            RecordFailure(source, attemptedAt, failedValidation.Issues);
            return new MenuSnapshotActivationResult(false, Current, failedValidation);
        }
    }

    public void RecordRejected(
        MenuSnapshotSource source,
        DateTimeOffset attemptedAt,
        MenuReleaseValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);
        if (validation.IsValid)
        {
            throw new ArgumentException("Only a rejected validation result can be recorded.", nameof(validation));
        }

        RecordFailure(source, attemptedAt, validation.Issues);
    }

    private void RecordFailure(
        MenuSnapshotSource source,
        DateTimeOffset attemptedAt,
        IEnumerable<MenuValidationIssue> issues)
    {
        var active = Current;
        var diagnostics = issues.Select(issue => new MenuDiagnostic(
            attemptedAt,
            source,
            issue.Severity,
            issue.Code,
            issue.Message,
            issue.Path)).ToImmutableArray();
        Volatile.Write(
            ref _status,
            new MenuSnapshotStatus(
                active.ReleaseId,
                active.Source,
                attemptedAt,
                LastAttemptSucceeded: false,
                diagnostics));
    }
}
