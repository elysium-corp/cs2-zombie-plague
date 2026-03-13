using CS2ZombiePlague.Data.Menus.Contracts;
using CS2ZombiePlague.Data.Weapons.Enums;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;

namespace CS2ZombiePlague.Data.Menus;

public sealed class WeaponCategoriesMenu(ISwiftlyCore core): IMenu
{
    private const string MenuTitle = "Menu.WeaponCategories.Title";

    private static readonly string[] CategoryKeys =
    [
        MenuGlobalVars.MenuCategoryPistols,
        MenuGlobalVars.MenuCategorySubmachineGuns,
        MenuGlobalVars.MenuCategoryRifles,
        MenuGlobalVars.MenuCategoryShotguns,
        MenuGlobalVars.MenuCategorySniperRifles,
        MenuGlobalVars.MenuCategoryMachineGuns,
        MenuGlobalVars.MenuCategoryEquipment
    ];
    
    public IMenuAPI Open(IPlayer player, IMenuAPI? parent = null)
    {
        var builder = Builder(player, parent);
        var menu = builder.Build();
        
        core.MenusAPI.OpenMenuForPlayer(player, menu);
        
        return menu;
    }

    public void OpenAll(Predicate<IPlayer>? predicate, IMenuAPI? parent = null)
    {
        var alivePlayers = core.PlayerManager.GetAlive();
        
        foreach (var player in alivePlayers)
        {
            var condition = predicate?.Invoke(player) ?? true;
            if (condition)
            {
                Open(player, parent);
            }
        }
    }
    
    public IMenuBuilderAPI Builder(IPlayer player, IMenuAPI? parent)
    {
        var locale = core.Translation.GetPlayerLocalizer(player);
        
        var builder = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle(locale[MenuTitle])
            .Design.SetMenuFooterVisible()
            .EnableExit()
            .SetPlayerFrozen()
            .EnableSound();
        
        if (parent != null)
        {
            builder.BindToParent(parent);
        }

        foreach (var category in CategoryKeys)
        {
            AddCategory(builder, locale, category, parent);
        }
        
        return builder;
    }

    private void AddCategory(IMenuBuilderAPI builder, ILocalizer locale, string categoryKey, IMenuAPI? parent)
    {
        var button = new ButtonMenuOption(locale[categoryKey]);

        button.Click += (_, args) =>
        {
            var clicker = args.Player;
            var weaponType = ConvertCategoryToType(categoryKey);
            var menuCategory = new WeaponCategoryMenu(core, weaponType);
            menuCategory.Open(clicker, parent);
            return ValueTask.CompletedTask;
        };

        builder.AddOption(button);
    }
    
    private WeaponType ConvertCategoryToType(string type)
    {
        return type switch
        {
            MenuGlobalVars.MenuCategoryPistols => WeaponType.Pistol,
            MenuGlobalVars.MenuCategorySubmachineGuns => WeaponType.SubmachineGun,
            MenuGlobalVars.MenuCategoryRifles => WeaponType.Rifle,
            MenuGlobalVars.MenuCategoryShotguns => WeaponType.Shotgun,
            MenuGlobalVars.MenuCategorySniperRifles => WeaponType.SniperRifle,
            MenuGlobalVars.MenuCategoryMachineGuns => WeaponType.MachineGun,
            MenuGlobalVars.MenuCategoryEquipment => WeaponType.Equipment,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }
}