namespace Admin.Api.Data;

public interface IPrivilege
{
    string Id { get; init; }

    string Group { get; init; }

    IReadOnlySet<string> Permissions { get; init; }
}