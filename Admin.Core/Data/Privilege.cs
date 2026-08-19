using Admin.Api.Data;

namespace Admin.Core.Data;

internal sealed class Privilege : IPrivilege
{
    public required string Id { get; init; }

    public required string Group { get; init; }

    public required IReadOnlySet<string> Permissions { get; init; }
    
    public string Key => $"{Group}.{Id}";
}