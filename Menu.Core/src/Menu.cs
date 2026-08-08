using Common.Di;
using Menu.Api;
using Menu.Core.Api;
using Menu.Core.Di;
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
    private readonly Lazy<MenuApi> _menuApi = GetRequiredServiceLazy<MenuApi>();

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IMenuApi, MenuApi>(IMenuApi.SharedApiKey, _menuApi.Value);
    }
}