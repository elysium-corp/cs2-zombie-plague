using Common.Di;
using Menu.Api;
using Menu.Core.Api;
using Menu.Core.Di;
using Menu.Core.Hud;
using SwiftlyS2.Shared;

namespace Menu.Core;

[PluginMetadata(
    Id = "Menu.Core",
    Version = "0.2.0",
    Name = "Elysium Menu Service",
    Author = "illusion & fdrinv",
    Description = "Общие текстовые меню и интерактивный Custom HUD"
)]
internal sealed partial class Menu(ISwiftlyCore core) : Plugin<MenuModule>(core)
{
    private readonly Lazy<MenuApi> _menuApi = GetRequiredServiceLazy<MenuApi>();
    private readonly Lazy<HudMenuService> _hudMenu = GetRequiredServiceLazy<HudMenuService>();

    protected override void OnStart()
    {
        _hudMenu.Value.Initialize();
    }

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IMenuApi, MenuApi>(IMenuApi.SharedApiKey, _menuApi.Value);
    }

    protected override void OnUnload()
    {
        _hudMenu.Value.Shutdown();
    }
}
