using System.Collections.Frozen;
using System.Collections.Immutable;
using Menu.Api.Enums;
using Menu.Core.Validation;

namespace Menu.Core.Runtime;

internal sealed class MenuRuntimeSnapshot
{
    public MenuRuntimeSnapshot(
        long releaseId,
        int schemaVersion,
        int menuCoreApiVersion,
        DateTimeOffset generatedAt,
        string checksum,
        MenuSnapshotSource source,
        DateTimeOffset loadedAt,
        IEnumerable<CompiledMenu> menus,
        IEnumerable<CompiledMenuCommand> commands,
        IEnumerable<MenuDiagnostic> diagnostics)
    {
        ArgumentNullException.ThrowIfNull(menus);
        ArgumentNullException.ThrowIfNull(commands);
        ArgumentNullException.ThrowIfNull(diagnostics);

        ReleaseId = releaseId;
        SchemaVersion = schemaVersion;
        MenuCoreApiVersion = menuCoreApiVersion;
        GeneratedAt = generatedAt;
        Checksum = checksum;
        Source = source;
        LoadedAt = loadedAt;
        Menus = menus.ToFrozenDictionary(static menu => menu.MenuKey, StringComparer.Ordinal);
        Commands = commands.ToFrozenDictionary(static command => command.LookupKey, StringComparer.Ordinal);
        Diagnostics = [.. diagnostics];
    }

    public static MenuRuntimeSnapshot Empty { get; } = new(
        releaseId: 0,
        schemaVersion: 0,
        menuCoreApiVersion: 0,
        generatedAt: default,
        checksum: string.Empty,
        source: MenuSnapshotSource.None,
        loadedAt: default,
        menus: [],
        commands: [],
        diagnostics: []);

    public long ReleaseId { get; }

    public int SchemaVersion { get; }

    public int MenuCoreApiVersion { get; }

    public DateTimeOffset GeneratedAt { get; }

    public string Checksum { get; }

    public MenuSnapshotSource Source { get; }

    public DateTimeOffset LoadedAt { get; }

    public FrozenDictionary<string, CompiledMenu> Menus { get; }

    public FrozenDictionary<string, CompiledMenuCommand> Commands { get; }

    public ImmutableArray<MenuDiagnostic> Diagnostics { get; }

    public bool TryGetMenu(string menuKey, out CompiledMenu menu)
    {
        return Menus.TryGetValue(menuKey, out menu!);
    }

    public bool TryGetCommand(MenuCommandKind kind, string alias, out CompiledMenuCommand command)
    {
        if (alias is null)
        {
            command = null!;
            return false;
        }

        var lookupKey = MenuIdentifier.CommandLookupKey(kind, alias);
        return Commands.TryGetValue(lookupKey, out command!);
    }
}
