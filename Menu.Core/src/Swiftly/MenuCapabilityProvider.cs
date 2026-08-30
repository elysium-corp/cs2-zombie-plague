using Menu.Api.Contracts;
using Menu.Core.Configuration;
using Microsoft.Extensions.Options;

namespace Menu.Core.Swiftly;

internal sealed class MenuCapabilityProvider(IOptions<MenuCoreConfig> options)
{
    private readonly MenuCapabilityManifest _manifest = new()
    {
        ServerKey = options.Value.ServerKey,
        MenuCoreVersion = "1.0.0",
        MenuCoreApiVersion = MenuContractVersions.MenuCoreApiVersion,
        SchemaVersion = MenuContractVersions.SchemaVersion,
        SwiftlyMenuApiVersion = "1.4.6-beta.8",
        ObservedAt = DateTimeOffset.UtcNow,
        MaximumNavigationDepth = options.Value.MaxNavigationDepth,
        ReservedCommands = options.Value.ReservedCommands.ToArray(),
        Features = new Dictionary<string, bool>(StringComparer.Ordinal)
        {
            [MenuFeatureKeys.Checkbox] = true, // Адаптируется через ToggleMenuOption.
            [MenuFeatureKeys.Slider] = true,
            [MenuFeatureKeys.Choice] = true,
            [MenuFeatureKeys.C4] = false,
            [MenuFeatureKeys.CustomKeyBinds] = true,
            [MenuFeatureKeys.ParentNavigation] = true,
            [MenuFeatureKeys.ExtraButtons] = true,
            [MenuFeatureKeys.OverlayOnly] = false,
            [MenuFeatureKeys.WelcomeScreen] = false,
            [MenuFeatureKeys.WrapNavigation] = false,
            [MenuFeatureKeys.ScrollCooldown] = false,
            [MenuFeatureKeys.OverrideColor] = true,
            [MenuFeatureKeys.MenuSound] = false,
            [MenuFeatureKeys.AccessTitle] = false,
            [MenuFeatureKeys.NextPageText] = false,
            [MenuFeatureKeys.PreviousPageText] = false,
            [MenuFeatureKeys.CurrentlySelectedText] = false,
            [MenuFeatureKeys.CenterMenuText] = false,
            [MenuFeatureKeys.SoundToggle] = true,
            [MenuFeatureKeys.AutoClose] = true,
        },
    };

    internal MenuCapabilityManifest Current => _manifest;
}
