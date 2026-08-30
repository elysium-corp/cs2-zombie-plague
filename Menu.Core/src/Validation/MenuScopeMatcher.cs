using Menu.Api.Contracts;
using Menu.Api.Enums;

namespace Menu.Core.Validation;

internal static class MenuScopeMatcher
{
    public static bool IsStructurallyValid(MenuScopeDefinition? scope)
    {
        if (scope is null)
        {
            return false;
        }

        return scope.Kind switch
        {
            MenuScopeKind.Global =>
                string.IsNullOrEmpty(scope.ServerKey) && string.IsNullOrEmpty(scope.ServerGroupKey),
            MenuScopeKind.Server =>
                MenuIdentifier.IsTechnicalKey(scope.ServerKey) && string.IsNullOrEmpty(scope.ServerGroupKey),
            MenuScopeKind.ServerGroup =>
                MenuIdentifier.IsTechnicalKey(scope.ServerGroupKey) && string.IsNullOrEmpty(scope.ServerKey),
            _ => false
        };
    }

    public static bool AppliesTo(
        MenuScopeDefinition scope,
        string serverKey,
        IReadOnlySet<string> serverGroups)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverKey);
        ArgumentNullException.ThrowIfNull(serverGroups);

        return scope.Kind switch
        {
            MenuScopeKind.Global => true,
            MenuScopeKind.Server => string.Equals(scope.ServerKey, serverKey, StringComparison.Ordinal),
            MenuScopeKind.ServerGroup =>
                scope.ServerGroupKey is not null && serverGroups.Contains(scope.ServerGroupKey),
            _ => false
        };
    }
}
