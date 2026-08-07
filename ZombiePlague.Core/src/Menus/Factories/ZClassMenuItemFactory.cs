using Menu.Api.Data.Contracts;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Config.Zombie;
using ZombiePlague.Core.Data.Entities.Registrator;
using ZombiePlague.Core.Store.Contracts;

namespace ZombiePlague.Core.Menus.Factories;

internal sealed class ZClassMenuItemFactory(
    IZClassRegistrator zClassRegistrator,
    IPlayerRepository playerRepository
) : IZClassMenuItemFactory
{
    public void OnZClassMenuAddOption(IPlayer player, DynamicOptionsMenu.MenuOptionsHolder holder)
    {
        var currentZClass = playerRepository.GetZClassId(player);
        var zClasses = zClassRegistrator.GetAllEnabled()
            .Where(zClass => zClass is not ZombieNemesis);

        foreach (var zClass in zClasses)
        {
            var option = BuildZClassButtonOption(currentZClass, zClass);
            holder.Add(option);
        }
    }

    private ButtonMenuOption BuildZClassButtonOption(string currentZClass, IZClassConfig zClass)
    {
        var isSelected = zClass.InternalName == currentZClass;
        var selectedText = isSelected ? " [текущий]" : "";
        var name = zClass.DisplayName;
        var text = $"{name}" + selectedText;
        var option = new ButtonMenuOption
        {
            Enabled = !isSelected,
            Text = text,
            Comment = zClass.Description
        };
        option.Click += (_, args) =>
        {
            var player = args.Player;
            
            playerRepository.SetZClassId(player, zClass.InternalName);
            
            player.SendChatAsync($"Вы успешно выбрали класс зомби: {name}");
            
            return ValueTask.CompletedTask;
        };
        return option;
    }
}