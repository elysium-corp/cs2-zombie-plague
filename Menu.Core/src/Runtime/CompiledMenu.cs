using System.Collections.Immutable;
using Menu.Api.Contracts;

namespace Menu.Core.Runtime;

internal sealed class CompiledMenu
{
    public CompiledMenu(MenuDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        Definition = definition;
        Items = [.. (definition.Items ?? Array.Empty<MenuItemDefinition>())];
    }

    public string MenuKey => Definition.MenuKey;

    public MenuDefinition Definition { get; }

    public ImmutableArray<MenuItemDefinition> Items { get; }
}
