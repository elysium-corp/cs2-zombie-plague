using Common.Di;
using Menu.Api;
using Menu.Api.Events;
using Menu.Core.Api;
using Menu.Core.Di;
using Menu.Core.Service;
using SwiftlyS2.Shared;

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
    
    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        var menuService = GetRequiredService<IMenuService>();
        var menuApi = new MenuApi(menuService, _eventSubscriber.Value);
        interfaceManager.AddSharedInterface<IMenuApi, MenuApi>(IMenuApi.SharedApiKey, menuApi);
    }
}