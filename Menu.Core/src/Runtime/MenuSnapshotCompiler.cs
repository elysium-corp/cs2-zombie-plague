using System.Text.Json;
using Menu.Api.Contracts;
using Menu.Core.Storage;
using Menu.Core.Validation;

namespace Menu.Core.Runtime;

internal sealed class MenuSnapshotCompiler
{
    public MenuRuntimeSnapshot Compile(
        MenuReleaseDefinition release,
        MenuReleaseValidationContext context,
        MenuSnapshotSource source,
        DateTimeOffset loadedAt,
        MenuReleaseValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(release);
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(validation);
        if (!validation.IsValid)
        {
            throw new ArgumentException("An invalid Release cannot be compiled.", nameof(validation));
        }

        // Отделяем snapshot от переданного DTO: IReadOnlyList в публичном API может
        // фактически быть изменяемым List. Round-trip выполняется только на cold path.
        var detachedJson = JsonSerializer.SerializeToUtf8Bytes(release, MenuJson.SerializerOptions);
        var detached = JsonSerializer.Deserialize<MenuReleaseDefinition>(detachedJson, MenuJson.SerializerOptions)
            ?? throw new JsonException("Release detached copy is null.");

        var menus = (detached.Menus ?? Array.Empty<MenuDefinition>())
            .Where(menu => menu is not null &&
                           MenuScopeMatcher.IsStructurallyValid(menu.Scope) &&
                           MenuScopeMatcher.AppliesTo(menu.Scope, context.ServerKey, context.ServerGroups))
            .Select(static menu => new CompiledMenu(menu));

        var commands = (detached.Commands ?? Array.Empty<MenuCommandDefinition>())
            .Where(command => command is not null && command.Enabled &&
                              MenuScopeMatcher.IsStructurallyValid(command.Scope) &&
                              MenuScopeMatcher.AppliesTo(command.Scope, context.ServerKey, context.ServerGroups))
            .Select(static command =>
            {
                var canonicalAlias = MenuIdentifier.CanonicalizeAlias(command.Alias);
                return new CompiledMenuCommand(
                    MenuIdentifier.CommandLookupKey(command.Kind, canonicalAlias),
                    canonicalAlias,
                    command.MenuKey,
                    command);
            });

        var diagnostics = validation.Warnings.Select(issue => new MenuDiagnostic(
            loadedAt,
            source,
            issue.Severity,
            issue.Code,
            issue.Message,
            issue.Path));

        return new MenuRuntimeSnapshot(
            detached.ReleaseId,
            detached.SchemaVersion,
            detached.MenuCoreApiVersion,
            detached.GeneratedAt,
            detached.Checksum!,
            source,
            loadedAt,
            menus,
            commands,
            diagnostics);
    }
}
