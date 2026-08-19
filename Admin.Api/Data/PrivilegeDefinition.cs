namespace Admin.Api.Data;

public sealed record PrivilegeDefinition(
    string Id,
    string Group,
    IReadOnlySet<string> Permissions
);