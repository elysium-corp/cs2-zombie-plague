using Menu.Api.Hud;
using Metrics.Api;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;
using ZombiePlague.Api.Data.Store;
using ZombiePlague.Api.Menus;
using ZombiePlague.Core.Config.Zombie;

namespace ZombiePlague.Core.Menus;

internal sealed class ZClassCardMenu(
    ISwiftlyCore core,
    IPlayerRepository playerRepository,
    IMetricsService metrics)
{
    private const string LayoutPath = "panorama/layout/custom_game/elysium/zombie_class_card.xml";
    private const string RootPanelId = "ZombieClassCard";
    private const string SelectButtonId = "ZClassCardSelect";
    private const string CloseButtonId = "ZClassCardClose";
    private const string ActiveImageClass = "is-active";

    private static readonly IReadOnlyDictionary<string, string> ImagePanels =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["zombie_cleric"] = "ZClassImageCleric",
            ["zombie_hunter"] = "ZClassImageHunter",
            ["zombie_assassin"] = "ZClassImageAssassin",
            ["zombie_heavy"] = "ZClassImageHeavy",
            ["zombie_smoker"] = "ZClassImageSmoker"
        };

    private IHudMenuApi? _hudMenu;
    private IDisposable? _registration;

    public void Initialize(IHudMenuApi hudMenu)
    {
        ArgumentNullException.ThrowIfNull(hudMenu);

        Uninitialize();

        _hudMenu = hudMenu;
        _registration = hudMenu.Register(
            new HudMenuDefinition(
                    ZombiePlagueMenuIds.ZClassCard,
                    LayoutPath,
                    RootPanelId
                )
                .AddButton(SelectButtonId, SelectClass)
                .AddButton(CloseButtonId, Close)
        );
    }

    public void Uninitialize()
    {
        _registration?.Dispose();
        _registration = null;
        _hudMenu = null;
    }

    public void Open(IPlayer player, IZClassConfig zClass)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(zClass);

        if (_hudMenu is null || !player.IsValid || player.IsFakeClient)
        {
            return;
        }

        var localizer = core.Translation.GetPlayerLocalizer(player);
        var isSelected = string.Equals(
            playerRepository.GetZClassId(player),
            zClass.InternalName,
            StringComparison.Ordinal
        );

        var view = new HudMenuView()
            .WithState(zClass)
            .SetDialogVariable("ZClassCardEyebrow", "value", localizer["Menu.ZClass.Card.Eyebrow"])
            .SetDialogVariable("ZClassCardTitle", "value", zClass.DisplayName)
            .SetDialogVariable("ZClassCardDescription", "value", zClass.Description)
            .SetDialogVariable("ZClassCardAbilitiesTitle", "value", localizer["Menu.ZClass.Card.Abilities"])
            .SetDialogVariable("ZClassCardAbilities", "value", BuildAbilities(localizer, zClass))
            .SetDialogVariable("ZClassCardHealth", "value", $"{localizer["Menu.ZClass.Card.Health"]}: {zClass.Health:N0}")
            .SetDialogVariable("ZClassCardSpeed", "value", $"{localizer["Menu.ZClass.Card.Speed"]}: {zClass.Speed:0}")
            .SetDialogVariable("ZClassCardGravity", "value", $"{localizer["Menu.ZClass.Card.Gravity"]}: {zClass.Gravity}")
            .SetDialogVariable(
                "ZClassCardSelectLabel",
                "value",
                localizer[isSelected ? "Menu.ZClass.Card.SelectedButton" : "Menu.ZClass.Card.SelectButton"]
            )
            .SetClass(SelectButtonId, "is-current", isSelected);

        foreach (var panelId in ImagePanels.Values.Append("ZClassImageFallback"))
        {
            var isActive = ImagePanels.TryGetValue(zClass.InternalName, out var selectedPanel)
                ? string.Equals(panelId, selectedPanel, StringComparison.Ordinal)
                : string.Equals(panelId, "ZClassImageFallback", StringComparison.Ordinal);

            view.SetClass(panelId, ActiveImageClass, isActive);
        }

        _hudMenu.Open(player, ZombiePlagueMenuIds.ZClassCard, view);
    }

    private void SelectClass(HudMenuButtonContext context)
    {
        if (context.State is not IZClassConfig zClass)
        {
            context.Menu.Close(context.Player);
            return;
        }

        var player = context.Player;
        var isAlreadySelected = string.Equals(
            playerRepository.GetZClassId(player),
            zClass.InternalName,
            StringComparison.Ordinal
        );

        if (!isAlreadySelected)
        {
            playerRepository.SetZClassId(player, zClass.InternalName);

            if (player.IsAuthorized && !player.IsFakeClient)
            {
                metrics.Track(
                    "class_selected",
                    player.SteamID,
                    new
                    {
                        class_id = zClass.InternalName,
                        class_name = zClass.DisplayName,
                        class_type = "zombie"
                    }
                );
            }

            var localizer = core.Translation.GetPlayerLocalizer(player);
            player.SendChatAsync($"{localizer["Menu.ZClass.Card.SelectedMessage"]}: {zClass.DisplayName}");
        }

        context.Menu.Close(player);
    }

    private static void Close(HudMenuButtonContext context)
    {
        context.Menu.Close(context.Player);
    }

    private static string BuildAbilities(ILocalizer localizer, IZClassConfig zClass)
    {
        if (zClass.Abilities.Count == 0)
        {
            return localizer["Menu.ZClass.Card.NoAbilities"];
        }

        return string.Join(
            " • ",
            zClass.Abilities.Select(ability => localizer[$"Menu.ZClass.Card.Ability.{ability}"])
        );
    }
}
