using Menu.Api.Enums;

namespace Menu.Core.Runtime;

internal sealed record MenuDiagnostic(
    DateTimeOffset ObservedAt,
    MenuSnapshotSource Source,
    MenuValidationSeverity Severity,
    string Code,
    string Message,
    string? Path = null);
