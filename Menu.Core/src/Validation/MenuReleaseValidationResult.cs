using System.Collections.Immutable;
using Menu.Api.Enums;
using Menu.Api.Results;

namespace Menu.Core.Validation;

internal sealed class MenuReleaseValidationResult
{
    private readonly ImmutableArray<MenuValidationIssue> _issues;

    public MenuReleaseValidationResult(IEnumerable<MenuValidationIssue> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);
        _issues = [.. issues];
    }

    public static MenuReleaseValidationResult Success { get; } = new([]);

    public bool IsValid => !_issues.Any(static issue => issue.Severity == MenuValidationSeverity.Error);

    public ImmutableArray<MenuValidationIssue> Issues => _issues;

    public ImmutableArray<MenuValidationIssue> Errors =>
        [.. _issues.Where(static issue => issue.Severity == MenuValidationSeverity.Error)];

    public ImmutableArray<MenuValidationIssue> Warnings =>
        [.. _issues.Where(static issue => issue.Severity == MenuValidationSeverity.Warning)];
}
