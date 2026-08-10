using Common.Di;
using CustomEquipment.Api;
using Menu.Api;
using MoneySystem.Api;
using Shop.Api;
using Shop.Core.Di;
using Shop.Core.SharedApi;
using SwiftlyS2.Shared;
using ZombiePlague.Api;

namespace Shop.Core;

[PluginMetadata(
    Id = "Shop.Core",
    Version = "0.1.0",
    Name = "[ZP] Shop",
    Author = "illusion & fdrinv",
    Description = "Provides a configurable shop for custom equipment"
)]
internal sealed partial class Shop(ISwiftlyCore core) : Plugin<ShopModule>(core)
{
    private readonly Lazy<ShopApi> _api = GetRequiredServiceLazy<ShopApi>();
    private readonly Lazy<ShopCoordinator> _coordinator = GetRequiredServiceLazy<ShopCoordinator>();

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IShopApi, ShopApi>(
            IShopApi.SharedApiKey,
            _api.Value
        );
    }

    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        BindSharedInterface<ICustomEquipmentApi>(interfaceManager, ICustomEquipmentApi.SharedApiKey);
        BindSharedInterface<IMenuApi>(interfaceManager, IMenuApi.SharedApiKey);
        BindSharedInterface<IMoneySystemPaymentApi>(interfaceManager, IMoneySystemPaymentApi.SharedApiKey);
        BindSharedInterface<IZombiePlagueApi>(interfaceManager, IZombiePlagueApi.SharedApiKey);
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
