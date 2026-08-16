using CustomKnife.Data.Models;
using CustomKnife.Data.Registrator;
using CustomKnife.Data.Services.Contracts;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using Menu.Api.Extensions;
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
    IKnifeService knifeService
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
        var localizer = Core.Translation.GetPlayerLocalizer(player);

        return design
            .SetMenuTitle(localizer[KnifeMenuTitle])
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }
    
    protected override void BuildOptions(IPlayer player, MenuOptionsCollection options)
    {
        var currentKnife = knifeService.GetKnife(player);
        
        var knives = knivesRegistry.GetAll();

        foreach (var knife in knives)
        {
            options.Add(BuildKnifeOption(currentKnife, knife));
        }
    }
    
    private ButtonMenuOption BuildKnifeOption(IKnife currentKnife, IKnife knife)
    {
        var isSelected = knife.InternalName == currentKnife.InternalName;

        var option = new ButtonMenuOption
        {
            Enabled = !isSelected,

            Text = isSelected
                ? $"{knife.DisplayName} [выбран]"
                : knife.DisplayName,

            Comment = knife.Description
        };

        option.Click += async (_, args) =>
        {
            var player = args.Player;

            await knifeService.SelectKnifeAsync(player, knife);

            await player.SendChatAsync($"Вы успешно выбрали нож: {knife.DisplayName}");

            core.MenusAPI.CloseActiveMenu(player);
        };

        return option;
    }
}