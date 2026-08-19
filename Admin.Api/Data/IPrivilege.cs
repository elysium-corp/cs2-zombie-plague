namespace Admin.Api.Data;

public interface IPrivilege
{
    string Id { get; }

    string Group { get; }
    
    string Key { get; }

    IReadOnlySet<string> Permissions { get; }
}