using Common.Di;
using CustomEquipment.Controllers;
using CustomEquipment.Api;
using CustomEquipment.Di;
using CustomEquipment.Services;
using CustomEquipment.SharedApi;
using SwiftlyS2.Shared;

namespace CustomEquipment;

[PluginMetadata(
    Id = "CustomEquipment.Core", 
    Version = "0.1.0", 
    Name = "[ZP] CustomEquipment",
    Author = "illusion & fdrinv",
    Description = "Provides custom equipment and a shared item delivery API")
]
internal sealed partial class CustomEquipment(ISwiftlyCore core) : Plugin<CustomEquipmentModule>(core)
{
    private readonly Lazy<IWeaponController> _itemController = GetRequiredServiceLazy<IWeaponController>();
    private readonly Lazy<IParticleController> _particleController = GetRequiredServiceLazy<IParticleController>();
    private readonly Lazy<IEquipmentService> _equipmentService = GetRequiredServiceLazy<IEquipmentService>();
    private readonly Lazy<IItemService> _itemService = GetRequiredServiceLazy<IItemService>();
    private readonly Lazy<CustomEquipmentApi> _api = GetRequiredServiceLazy<CustomEquipmentApi>();

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<ICustomEquipmentApi, CustomEquipmentApi>(
            ICustomEquipmentApi.SharedApiKey,
            _api.Value
        );
    }
    
    protected override void OnStart()
    {
        _itemService.Value.Initialize();
        _equipmentService.Value.Initialize();
        _itemController.Value.Initialize();
        _particleController.Value.Initialize();
    }
}
