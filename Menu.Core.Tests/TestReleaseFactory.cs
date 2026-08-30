using System.Text.Json;
using Menu.Api.Contracts;
using Menu.Api.Enums;
using Menu.Core.Storage;
using Menu.Core.Validation;

namespace Menu.Core.Tests;

internal static class TestReleaseFactory
{
    private static readonly DateTimeOffset GeneratedAt =
        new(2026, 8, 30, 12, 0, 0, TimeSpan.Zero);

    internal static MenuReleaseDefinition Release(
        long releaseId = 1,
        IReadOnlyList<MenuDefinition>? menus = null,
        IReadOnlyList<MenuCommandDefinition>? commands = null)
    {
        var release = new MenuReleaseDefinition
        {
            ReleaseId = releaseId,
            GeneratedAt = GeneratedAt.AddSeconds(releaseId),
            Menus = menus ?? [Menu()],
            Commands = commands ?? Array.Empty<MenuCommandDefinition>()
        };

        return WithChecksum(release);
    }

    internal static MenuReleaseDefinition WithChecksum(MenuReleaseDefinition release)
    {
        return release with { Checksum = MenuJson.ComputeChecksum(release) };
    }

    internal static MenuDefinition Menu(
        string menuKey = "main",
        MenuLifecycleStatus status = MenuLifecycleStatus.Published,
        MenuScopeDefinition? scope = null,
        MenuReferenceDefinition? parent = null,
        IReadOnlyList<MenuItemDefinition>? items = null,
        string? providerKey = null)
    {
        return new MenuDefinition
        {
            MenuKey = menuKey,
            Revision = 1,
            Status = status,
            ProviderKey = providerKey,
            Title = Text($"Menu {menuKey}"),
            Scope = scope ?? GlobalScope(),
            Parent = parent,
            Items = items ?? Array.Empty<MenuItemDefinition>()
        };
    }

    internal static MenuItemDefinition TextItem(
        string itemKey,
        MenuActionDefinition? action = null,
        MenuItemKind kind = MenuItemKind.Text)
    {
        return new MenuItemDefinition
        {
            ItemKey = itemKey,
            Kind = kind,
            Text = Text(itemKey),
            Action = action
        };
    }

    internal static MenuActionDefinition OpenMenu(string menuKey)
    {
        return new MenuActionDefinition
        {
            Kind = MenuActionKind.OpenMenu,
            TargetMenu = new MenuReferenceDefinition { MenuKey = menuKey }
        };
    }

    internal static MenuActionDefinition OpenProviderMenu(string providerKey, string menuKey)
    {
        return new MenuActionDefinition
        {
            Kind = MenuActionKind.OpenProviderMenu,
            TargetMenu = new MenuReferenceDefinition
            {
                ProviderKey = providerKey,
                MenuKey = menuKey
            }
        };
    }

    internal static MenuActionDefinition Back()
    {
        return new MenuActionDefinition { Kind = MenuActionKind.Back };
    }

    internal static MenuCommandDefinition Command(
        string commandKey,
        string alias,
        MenuCommandKind kind = MenuCommandKind.Chat,
        string menuKey = "main",
        MenuScopeDefinition? scope = null)
    {
        return new MenuCommandDefinition
        {
            CommandKey = commandKey,
            Alias = alias,
            Kind = kind,
            MenuKey = menuKey,
            Scope = scope ?? GlobalScope()
        };
    }

    internal static LocalizedText Text(string value)
    {
        return new LocalizedText { Default = value };
    }

    internal static MenuScopeDefinition GlobalScope()
    {
        return new MenuScopeDefinition { Kind = MenuScopeKind.Global };
    }

    internal static MenuScopeDefinition ServerScope(string serverKey)
    {
        return new MenuScopeDefinition
        {
            Kind = MenuScopeKind.Server,
            ServerKey = serverKey
        };
    }

    internal static MenuScopeDefinition GroupScope(string groupKey)
    {
        return new MenuScopeDefinition
        {
            Kind = MenuScopeKind.ServerGroup,
            ServerGroupKey = groupKey
        };
    }

    internal static MenuReleaseValidationContext Context(
        ProviderValidationCatalog? providers = null,
        IReadOnlyDictionary<string, bool>? features = null,
        int maximumNavigationDepth = 16)
    {
        var supportedFeatures = features ?? new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            [MenuFeatureKeys.Checkbox] = true,
            [MenuFeatureKeys.Choice] = true,
            [MenuFeatureKeys.Slider] = true,
            [MenuFeatureKeys.C4] = false,
            [MenuFeatureKeys.CustomKeyBinds] = true,
            [MenuFeatureKeys.ParentNavigation] = true,
            [MenuFeatureKeys.ExtraButtons] = true,
            [MenuFeatureKeys.OverlayOnly] = false,
            [MenuFeatureKeys.WelcomeScreen] = false
        };

        return new MenuReleaseValidationContext(
            "zombie-1",
            ["zombie", "public"],
            new MenuCapabilityManifest
            {
                ServerKey = "zombie-1",
                MenuCoreVersion = "1.0.0-test",
                SwiftlyMenuApiVersion = "1.4.6-beta.8",
                Features = supportedFeatures,
                ObservedAt = GeneratedAt
            },
            providers ?? ProviderValidationCatalog.Empty,
            ["sw_menu_reload", "sw_menu_status", "sw_menu_validate"],
            maximumNavigationDepth);
    }

    internal static JsonElement Json(object value)
    {
        return JsonSerializer.SerializeToElement(value);
    }
}
