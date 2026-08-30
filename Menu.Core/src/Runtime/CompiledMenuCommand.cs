using Menu.Api.Contracts;

namespace Menu.Core.Runtime;

internal sealed record CompiledMenuCommand(
    string LookupKey,
    string CanonicalAlias,
    string MenuKey,
    MenuCommandDefinition Definition);
