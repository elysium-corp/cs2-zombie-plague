using CustomKnife.Data.Models;
using CustomKnife.Data.Services.Contracts;
using CustomKnife.Data.Utils;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace CustomKnife.Data.Services;

public class KnifeMenuService(ISwiftlyCore core, IKnifeService knifeService) : IKnifeMenuService
{
    public void Show(IPlayer player)
    {
        core.MenusAPI.OpenMenuForPlayer(player, Create());
    }
    
    private IMenuAPI Create()
    {
        var builder = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle("Выбери нож")
            .EnableSound();
        
        foreach (var knife in CustomKnife.RegisteredKnifes)
        {
            AddKnifeOption(builder, knife);
        }
        
        return builder.Build();
    }
    
    private void AddKnifeOption(IMenuBuilderAPI builder, IKnife knife)
    {
        var button = new ButtonMenuOption($"{knife.DisplayName} {HtmlHelper.TextWithColor(knife.Description, "#FFFF00")}");
        
        button.Click += async (_, args) =>
        {
            var player = args.Player;

            knifeService.ChangeKnife(player, knife);
        };
        
        builder.AddOption(button);
    }
}