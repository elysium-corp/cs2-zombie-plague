using Common.Di;
using CustomKnife.Data.Menus;
using CustomKnife.Di;
using Menu.Api;
using SwiftlyS2.Shared;
using ZombiePlague.Api;

namespace CustomKnife;

[PluginMetadata(
    Id = "CustomKnife.Core",
    Version = "0.1.0",
    Name = "[ZP] CustomKnife",
    Author = "illusion & fdrinv",
    Description = "Adds a system of custom knives"
)]
internal sealed partial class CustomKnife(ISwiftlyCore core) : Plugin<CustomKnifeModule>(core)
{
    private readonly Lazy<CustomKnifeCoordinator> _coordinator = GetRequiredServiceLazy<CustomKnifeCoordinator>();
    private readonly Lazy<MenuApiBridge> _menuApiBridge = GetRequiredServiceLazy<MenuApiBridge>();
    
    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        BindSharedInterface<IZombiePlagueApi>(interfaceManager, IZombiePlagueApi.SharedApiKey);
    }

    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        var menuApi = interfaceManager.GetSharedInterface<IMenuApi>(IMenuApi.SharedApiKey);
        _menuApiBridge.Value.Initialize(menuApi);
    }

    protected override void OnReady()
    {
        _coordinator.Value.Start();
    }

    protected override void OnUnload()
    {
        _coordinator.Value.Stop();
    }
}