using Common.Di;
using Localization.Api;
using SupplyBox.Data.Entity;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace SupplyBox.Services;

internal sealed class SupplyBoxMenuService(
    ISwiftlyCore core,
    SupplyBoxMapConfigService mapConfigService,
    SupplyBoxEditService supplyBoxEditService,
    ILocalizationApi localization)
{
    private const int RotationDegree = 10;
    
    public void ShowMainMenu(IPlayer player)
    {
        var menu = GetMainMenu(player);
        
        core.MenusAPI.OpenMenuForPlayer(player, menu);
    }

    private IMenuAPI GetMainMenu(IPlayer player)
    {
        var builder = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle(localization.GetForPlayerOrKey(player, "SupplyBox.Editor.Title"))
            .AddOption(AddSupplyBoxOption(player))
            .AddOption(RemoveSupplyBoxOption(player))
            .EnableSound();

        return builder.Build();
    }

    private IMenuAPI GetAddSupplyBoxMenu(IPlayer player)
    {
        var container = DependencyResolver.GetRequiredService<SupplyBoxEntityTemplate>();
        core.Scheduler.NextWorldUpdateAsync(() => container.Spawn(player));
        
        var button1 = new ButtonMenuOption(localization.GetForPlayerOrKey(player, "SupplyBox.Editor.RotateRight"));
        button1.Click += async (sender, args) =>
        {
            container.Rotation += new Vector(0,-RotationDegree,0);
        };
        
        var button2 = new ButtonMenuOption(localization.GetForPlayerOrKey(player, "SupplyBox.Editor.RotateLeft"));
        button2.Click += async (sender, args) =>
        {
            container.Rotation += new Vector(0,RotationDegree,0);
        };
        
        var button3 = new ButtonMenuOption(localization.GetForPlayerOrKey(player, "SupplyBox.Editor.Cancel"));
        button3.Click += async (sender, args) =>
        {
            await core.Scheduler.NextWorldUpdateAsync(() =>
            {
                container.Destroy();
            });
            core.MenusAPI.CloseActiveMenu(args.Player);
        };
        
        var button4 = new ButtonMenuOption(localization.GetForPlayerOrKey(player, "SupplyBox.Editor.Install"));
        button4.Click += async (sender, args) =>
        {
            supplyBoxEditService.AddSupplyBoxEntity(container);
            await core.Scheduler.NextWorldUpdateAsync(() =>
            {
                container.Destroy();
            });
            core.MenusAPI.CloseActiveMenu(args.Player);
        };
        
        var menu = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle(localization.GetForPlayerOrKey(player, "SupplyBox.Editor.AddTitle"))
            .EnableSound()
            .AddOption(button1)
            .AddOption(button2)
            .AddOption(button3)
            .AddOption(button4)
            .Build();
        
        container.SetMenu(menu);
        
        return menu;
    }

    private IMenuAPI GetRemoveSupplyBoxMenu(IPlayer player)
    {
        var menu = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle(localization.GetForPlayerOrKey(player, "SupplyBox.Editor.RemoveTitle"))
            .EnableSound();

        var supplyBoxesList = mapConfigService.GetSnapshot();
        if (supplyBoxesList.Count == 0)
        {
            return menu.Build();
        }
        
        foreach (var supplyBox in supplyBoxesList)
        {
            var button = new ButtonMenuOption(localization.GetForPlayerOrKey(
                player,
                "SupplyBox.Editor.RemoveItem",
                new Dictionary<string, string> { ["index"] = supplyBox.Index.ToString() }));
            button.Click += async (sender, args) =>
            {
                supplyBoxEditService.RemoveSupplyBoxEntity(supplyBox);
                core.MenusAPI.CloseActiveMenu(args.Player);
            };
            
            menu.AddOption(button);
        }
        
        return menu.Build();
    }
    
    private IMenuOption AddSupplyBoxOption(IPlayer player)
    {
        var button = new ButtonMenuOption();
        
        button.Text = localization.GetForPlayerOrKey(player, "SupplyBox.Editor.Create");

        button.Click += async (_, args) =>
        {
            if (!args.Player.IsAlive)
            {
                return;
            }
            
            core.MenusAPI.CloseActiveMenu(args.Player);
            core.MenusAPI.OpenMenuForPlayer(args.Player, GetAddSupplyBoxMenu(args.Player));
        };

        return button;
    }
    
    private IMenuOption RemoveSupplyBoxOption(IPlayer player)
    {
        var button = new ButtonMenuOption();
        
        button.Text = localization.GetForPlayerOrKey(player, "SupplyBox.Editor.Remove");

        button.Click += async (_, args) =>
        {
            core.MenusAPI.CloseActiveMenu(args.Player);
            core.MenusAPI.OpenMenuForPlayer(args.Player, GetRemoveSupplyBoxMenu(args.Player));
        };

        return button;
    }
}
