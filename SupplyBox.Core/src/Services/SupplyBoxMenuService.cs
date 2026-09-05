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
    ILocalizationApi localization) : IDisposable
{
    private bool _disposed;
    private readonly HashSet<SupplyBoxEntityTemplate> _previews = [];

    public void Dispose()
    {
        _disposed = true;
        ClearPreviews();
    }

    public void ClearPreviews()
    {
        foreach (var preview in _previews) preview.Dispose();
        _previews.Clear();
    }

    private bool CanEdit(IPlayer player) => !_disposed && player.IsValid && core.Permission.PlayerHasPermission(player.SteamID, SupplyBox.EditorPermission);
    private const int RotationDegree = 10;
    
    public void ShowMainMenu(IPlayer player)
    {
        if (!CanEdit(player)) return;
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
        _previews.RemoveWhere(preview => preview.Entity is not { IsValidEntity: true });
        _previews.Add(container);
        core.Scheduler.NextWorldUpdate(() => { if (!_disposed) container.Spawn(player); });
        
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
            if (!CanEdit(args.Player)) return;
            container.Destroy();
            core.MenusAPI.CloseActiveMenu(args.Player);
        };
        
        var button4 = new ButtonMenuOption(localization.GetForPlayerOrKey(player, "SupplyBox.Editor.Install"));
        button4.Click += async (sender, args) =>
        {
            if (!CanEdit(args.Player)) return;
            var steamId = args.Player.SteamID;
            var activeMenu = core.MenusAPI.GetCurrentMenu(args.Player);
            button4.Enabled = false;
            var saved = await supplyBoxEditService.AddSupplyBoxEntity(container);
            if (_disposed) return;
            core.Scheduler.NextWorldUpdate(() =>
            {
                if (_disposed) return;
                container.Destroy();
                if (args.Player.IsValid && args.Player.SteamID == steamId)
                {
                    args.Player.SendChat(saved ? "Точка ящика сохранена в БД." : "Не удалось сохранить точку: проверьте подключение к БД.");
                    if (core.MenusAPI.GetCurrentMenu(args.Player) == activeMenu)
                        core.MenusAPI.CloseActiveMenu(args.Player);
                }
            });
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
                if (!CanEdit(args.Player)) return;
                var steamId = args.Player.SteamID;
                var activeMenu = core.MenusAPI.GetCurrentMenu(args.Player);
                button.Enabled = false;
                var saved = await supplyBoxEditService.RemoveSupplyBoxEntity(supplyBox);
                if (_disposed) return;
                core.Scheduler.NextWorldUpdate(() =>
                {
                    if (_disposed || !args.Player.IsValid || args.Player.SteamID != steamId) return;
                    args.Player.SendChat(saved ? "Точка удалена из БД." : "Не удалось удалить точку: проверьте подключение к БД.");
                    if (core.MenusAPI.GetCurrentMenu(args.Player) == activeMenu)
                        core.MenusAPI.CloseActiveMenu(args.Player);
                });
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
            if (!CanEdit(args.Player) || !args.Player.IsAlive)
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
            if (!CanEdit(args.Player)) return;
            core.MenusAPI.CloseActiveMenu(args.Player);
            core.MenusAPI.OpenMenuForPlayer(args.Player, GetRemoveSupplyBoxMenu(args.Player));
        };

        return button;
    }
}
