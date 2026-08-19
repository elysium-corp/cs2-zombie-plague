namespace Admin.Api.Data;

public interface IPrivilege
{
    string Id { get; }

    string Group { get; }

    IReadOnlySet<string> Permissions { get; }
}