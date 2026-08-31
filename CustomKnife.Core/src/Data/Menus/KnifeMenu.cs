using CustomKnife.Data.Models;
using CustomKnife.Data.Registrator;
using CustomKnife.Data.Services.Contracts;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using Menu.Api.Extensions;
using Localization.Api;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Menus;

namespace CustomKnife.Data.Menus;

internal sealed class KnifeMenu(
    ISwiftlyCore core, 
    IMenuExtensionDispatcher extensionDispatcher,
    IKnivesRegistry knivesRegistry,
    IKnifeService knifeService,
    ILocalizationApi localization
) : DynamicOptionsMenu(core, extensionDispatcher)
{
    public override string Id => ZombiePlagueMenuIds.Knife;
    
    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.CT;
    
    protected override IReadOnlyCollection<string> Commands { get; } =
    [
        "knife",
        "zknife",
        "лтшау",
        "ялтшау",
        "нож",
        "yj;"
    ];
    
    private const string KnifeMenuTitle = "Menu.Knife.Title";
    
    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle(localization.GetForPlayer(player, KnifeMenuTitle) ?? "Knives")
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }
    
    protected override void BuildOptions(IPlayer player, MenuOptionsCollection options)
    {
        var currentKnife = knifeService.GetKnife(player);
        
        var knives = knivesRegistry.GetAll();

        foreach (var knife in knives)
        {
            options.Add(BuildKnifeOption(player, currentKnife, knife));
        }
    }
    
    private ButtonMenuOption BuildKnifeOption(IPlayer player, IKnife currentKnife, IKnife knife)
    {
        var isSelected = knife.InternalName == currentKnife.InternalName;
        var knifeName = LocalizeKnifeField(player, knife, "Name", knife.DisplayName);
        var knifeDescription = LocalizeKnifeField(player, knife, "Description", knife.Description);

        var option = new ButtonMenuOption
        {
            Enabled = !isSelected,

            Text = isSelected
                ? localization.GetForPlayer(
                      player,
                      "Menu.Knife.Selected",
                      new Dictionary<string, string> { ["knife"] = knifeName })
                  ?? knifeName
                : knifeName,

            Comment = knifeDescription
        };

        option.Click += async (_, args) =>
        {
            var player = args.Player;

            knifeService.SelectKnife(player, knife);

            var message = localization.GetForPlayer(
                player,
                "Menu.Knife.SelectionSuccess",
                new Dictionary<string, string> { ["knife"] = knifeName })
                          ?? knifeName;
            await player.SendChatAsync(message);

            core.MenusAPI.CloseActiveMenu(player);
        };

        return option;
    }

    private string LocalizeKnifeField(IPlayer player, IKnife knife, string field, string fallback)
    {
        return localization.GetForPlayer(player, $"CustomKnife.{knife.InternalName}.{field}") ?? fallback;
    }
}
