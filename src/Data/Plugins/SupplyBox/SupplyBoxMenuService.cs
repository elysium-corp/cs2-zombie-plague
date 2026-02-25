using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Plugins.SupplyBox;

public sealed class SupplyBoxMenuService(ISwiftlyCore core, SupplyBoxMapConfigService mapConfigService, SupplyBoxEditService supplyBoxEditService)
{
    private const int RotationDegree = 10;
    
    public void ShowMainMenu(IPlayer player)
    {
        var menu = GetMainMenu();
        
        core.MenusAPI.OpenMenuForPlayer(player, menu);
    }

    private IMenuAPI GetMainMenu()
    {
        var builder = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle("Меню контейнеров")
            .AddOption(AddSupplyBoxOption())
            .AddOption(RemoveSupplyBoxOption())
            .EnableSound();

        return builder.Build();
    }

    private IMenuAPI GetAddSupplyBoxMenu(IPlayer player)
    {
        var container = new SupplyBoxEntityTemplate(player);
        core.Scheduler.NextWorldUpdateAsync(() => container.Spawn());
        
        var button1 = new ButtonMenuOption("Повернуть вправо на 10°");
        button1.Click += async (sender, args) =>
        {
            container.Rotation += new Vector(0,-RotationDegree,0);
        };
        
        var button2 = new ButtonMenuOption("Повернуть влево на 10°");
        button2.Click += async (sender, args) =>
        {
            container.Rotation += new Vector(0,RotationDegree,0);
        };
        
        var button3 = new ButtonMenuOption("<font color='#FF0000'>Отменить</font>");
        button3.Click += async (sender, args) =>
        {
            await core.Scheduler.NextWorldUpdateAsync(() =>
            {
                container.Destroy();
            });
            core.MenusAPI.CloseActiveMenu(args.Player);
        };
        
        var button4 = new ButtonMenuOption("<font color='#008000'>Установить</font>");
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
            .Design.SetMenuTitle("Добавления контейнера")
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
            .Design.SetMenuTitle("Удаление контейнеров")
            .EnableSound();

        var supplyBoxesList = mapConfigService.SupplyBoxesData;
        if (supplyBoxesList == null)
        {
            return menu.Build();
        }
        
        foreach (var supplyBox in supplyBoxesList)
        {
            var button = new ButtonMenuOption($"Удалить контейнер {supplyBox.Index}");
            button.Click += async (sender, args) =>
            {
                supplyBoxEditService.RemoveSupplyBoxEntity(supplyBox);
                core.MenusAPI.CloseActiveMenu(args.Player);
            };
            
            menu.AddOption(button);
        }
        
        return menu.Build();
    }
    
    private IMenuOption AddSupplyBoxOption()
    {
        var button = new ButtonMenuOption();
        
        button.Text = "Создать контейнер";

        button.Click += async (_, args) =>
        {
            core.MenusAPI.CloseActiveMenu(args.Player);
            core.MenusAPI.OpenMenuForPlayer(args.Player, GetAddSupplyBoxMenu(args.Player));
        };

        return button;
    }
    
    private IMenuOption RemoveSupplyBoxOption()
    {
        var button = new ButtonMenuOption();
        
        button.Text = "Удалить контейнеры";

        button.Click += async (_, args) =>
        {
            core.MenusAPI.CloseActiveMenu(args.Player);
            core.MenusAPI.OpenMenuForPlayer(args.Player, GetRemoveSupplyBoxMenu(args.Player));
        };

        return button;
    }
}