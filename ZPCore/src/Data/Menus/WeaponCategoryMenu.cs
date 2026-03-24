using ZPCore.Data.Menus.Contracts;
using ZPCore.Data.Weapons;
using ZPCore.Data.Weapons.Contracts;
using ZPCore.Data.Weapons.Enums;
using ZPCore.Data.Weapons.Mappers;
using ZPCore.Di;
using ZPCore.Service;
using ZPCore.Utils;
using ZPCore.Utils.Helpers;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Menus;

internal class WeaponCategoryMenu(ISwiftlyCore core, WeaponType type) : IMenu
{
    private readonly IWeaponRegistrator _weaponRegistrator = DependencyManager.GetService<IWeaponRegistrator>();
    private readonly WeaponService _weaponService = DependencyManager.GetService<WeaponService>();

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
            .Design.SetMenuTitle(locale[ConvertTypeToCategory()])
            .Design.SetMenuFooterVisible()
            .EnableExit()
            .SetPlayerFrozen()
            .EnableSound();

        if (parent != null)
        {
            builder.BindToParent(parent);
        }

        var weaponsByCategory = _weaponRegistrator
            .GetWeaponsByType(type)
            ?.OrderBy(w => w.Coast) ?? throw new ArgumentNullException();

        foreach (var weapon in weaponsByCategory)
        {
            AddCategory(builder, weapon);
        }

        return builder;
    }

    private void AddCategory(IMenuBuilderAPI builder, IWeaponPurchasable weapon)
    {
        var itemRarityColor = weapon.WeaponRarity.MapToRarityColor().Color;
        var weaponName = HtmlHelper.TextWithColor(weapon.DisplayName, itemRarityColor);
        var coast = weapon.Coast;
        var itemName = $"{weaponName} [{coast}$]";
        
        var button = new ButtonMenuOption(itemName);

        button.Click += (_, args) =>
        {
            var clicker = args.Player;

            if (TryBuyWeapon(clicker, weapon))
            {
                core.MenusAPI.CloseActiveMenu(clicker);
            }

            return ValueTask.CompletedTask;
        };

        builder.AddOption(button);
    }

    private string ConvertTypeToCategory()
    {
        return type switch
        {
            WeaponType.Pistol => MenuGlobalVars.MenuCategoryPistols,
            WeaponType.SubmachineGun => MenuGlobalVars.MenuCategorySubmachineGuns,
            WeaponType.Rifle => MenuGlobalVars.MenuCategoryRifles,
            WeaponType.Shotgun => MenuGlobalVars.MenuCategoryShotguns,
            WeaponType.SniperRifle => MenuGlobalVars.MenuCategorySniperRifles,
            WeaponType.MachineGun => MenuGlobalVars.MenuCategoryMachineGuns,
            WeaponType.Equipment => MenuGlobalVars.MenuCategoryEquipment,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private bool TryBuyWeapon(IPlayer player, IWeaponPurchasable weapon)
    {
        var coast = weapon.Coast;
        var moneyService = player.Controller.InGameMoneyServices;

        if (moneyService == null)
        {
            return false;
        }

        var account = moneyService.Account;

        if (coast > account)
        {
            return false;
        }

        moneyService.Account -= coast;
        moneyService.AccountUpdated();

        core.Scheduler.NextTick(() =>
        {
            if (weapon.WeaponType != WeaponType.Equipment)
            {
                _weaponService.GiveWeapon(player, weapon.InternalName);
            }

            _weaponService.GiveGrenade(player, weapon.InternalName);
        });

        return true;
    }
}