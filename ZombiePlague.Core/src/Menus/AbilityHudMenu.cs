using Localization.Api;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using Menu.Api.Extensions;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Experimental.AbilityHud;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Menus;

internal sealed class AbilityHudMenu(ISwiftlyCore core, IMenuExtensionDispatcher extensionDispatcher,
    AbilityHudSettings settings, AbilityHudService hud, Func<ILocalizationApi> localization)
    : DynamicOptionsMenu(core, extensionDispatcher)
{
    public override string Id => "zombie-plague.menu.ability-hud";
    protected override IReadOnlyCollection<string> Commands { get; } = ["hud", "zp_hud"];

    protected override bool CanOpenCore(IPlayer player)
    {
        var error = !hud.IsRunning ? "Menu.AbilityHud.Disabled"
            : !settings.HasSession(player.SteamID) ? "Menu.AbilityHud.Unavailable" : null;
        if (error is null) return true;
        player.SendChatAsync(Text(player, error));
        return false;
    }

    protected override IMenuAPI Build(IPlayer player)
    {
        var menu = base.Build(player);
        menu.Tag = AbilityHudSettings.PreviewMenuTag;
        return menu;
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design) => design
        .SetMenuTitle(Text(player, "Menu.AbilityHud.Title"))
        .Design.SetMenuFooterVisible(true)
        .Design.EnableAutoAdjustVisibleItems();

    protected override void BuildOptions(IPlayer player, MenuOptionsCollection options)
    {
        var current = settings.Get(player.SteamID);
        var scale = new SelectorMenuOption<int>(Text(player, "Menu.AbilityHud.Scale"), AbilityHudPreferences.Scales,
            Array.IndexOf(AbilityHudPreferences.Scales, current.ScalePercent), value => value + "%");
        scale.Comment = Text(player, "Menu.AbilityHud.PreviewHint");
        scale.SelectionChanged += (_, args) => settings.Update(args.Player.SteamID, value => value with { ScalePercent = args.NewValue });
        options.Add(scale);

        var position = new SelectorMenuOption<string>(Text(player, "Menu.AbilityHud.Position"), AbilityHudPreferences.Positions,
            Array.IndexOf(AbilityHudPreferences.Positions, current.Position),
            value => Text(player, "Menu.AbilityHud.Position." + value), itemMaxWidth: 18);
        position.SelectionChanged += (_, args) => settings.Update(args.Player.SteamID, value => value with { Position = args.NewValue });
        options.Add(position);

        var reset = new ButtonMenuOption(Text(player, "Menu.AbilityHud.Reset"));
        reset.Click += (_, args) =>
        {
            settings.Reset(args.Player.SteamID);
            scale.SetSelectedIndex(args.Player, Array.IndexOf(AbilityHudPreferences.Scales, AbilityHudPreferences.DefaultScale));
            position.SetSelectedIndex(args.Player, Array.IndexOf(AbilityHudPreferences.Positions, AbilityHudPreferences.DefaultPosition));
            return ValueTask.CompletedTask;
        };
        options.Add(reset);

        var close = new ButtonMenuOption(Text(player, "Menu.AbilityHud.Done"));
        close.Click += (_, args) =>
        {
            core.MenusAPI.CloseActiveMenu(args.Player);
            return ValueTask.CompletedTask;
        };
        options.Add(close);
    }

    private string Text(IPlayer player, string key) => localization().GetForPlayerOrKey(player, LocalizationKey.Canonicalize(key));
}
