using Common.Di;
using Menu.Api;
using Menu.Api.Data.Contracts;
using Menu.Api.Events;
using Menu.Core.Api;
using Menu.Core.Data;
using Menu.Core.Data.Menus;
using Menu.Core.Di;
using Menu.Core.Service;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Menus;

namespace Menu.Core;

[PluginMetadata(
    Id = "Menu.Core", 
    Version = "0.1.0", 
    Name = "[ZP] Custom menus", 
    Author = "illusion & fdrinv",
    Description = "Added custom equipment"
)]
internal sealed partial class Menu(ISwiftlyCore core) : Plugin<MenuModule>(core)
{
    private readonly Lazy<IEventSubscriber> _eventSubscriber = GetRequiredServiceLazy<IEventSubscriber>();
    private readonly Lazy<IEventPublisher> _eventPublisher = GetRequiredServiceLazy<IEventPublisher>();
    
    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        var menuService = GetRequiredService<IMenuService>();
        var menuApi = new MenuApi(menuService, _eventSubscriber.Value);
        interfaceManager.AddSharedInterface<IMenuApi, MenuApi>(IMenuApi.SharedApiKey, menuApi);
    }
    
    protected override void OnReady()
    {
        _eventSubscriber.Value.OnMenuAddOption += OnMenuAddOption;
        
        Core.Command.RegisterCommand(
            commandName: "menu",
            handler: MenuHandler,
            registerRaw: true
        );
    }

    private void MenuHandler(ICommandContext context)
    {
        var player = context.Sender;
        
        if (player == null) return;

        if (!context.IsSentByPlayer) return;

        var menu = new Main(core, _eventPublisher.Value);
        
        menu.Open(player);
    }
    
    private void OnMenuAddOption(Type menuType, DynamicOptionsMenu.MenuOptionsHolder holder)
    {
        var option1 = new ButtonMenuOption();
        option1.Text = "menuOptions 1 [priority 3]";
        var option2 = new ButtonMenuOption();
        option2.Text = "menuOptions 2 [priority 10]";
        
        holder.Add(option1, 3);
        holder.Add(option2, 10);
    }
}