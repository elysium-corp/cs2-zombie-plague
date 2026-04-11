using Common.Di;
using CustomEquipment.Controllers;
using CustomEquipment.Data.Equipments.Weapons.Grenades;
using CustomEquipment.Di;
using CustomEquipment.Services;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;

namespace CustomEquipment;

[PluginMetadata(
    Id = "CustomEquipment.Core", 
    Version = "0.1.0", 
    Name = "[ZP] CustomEquipment",
    Author = "illusion & fdrinv",
    Description = "Added custom equipment")
]
internal sealed partial class CustomEquipment(ISwiftlyCore core) : Plugin<CustomEquipmentModule>(core)
{
    private readonly Lazy<IWeaponController> _itemController = GetRequiredServiceLazy<IWeaponController>();
    private readonly Lazy<IParticleController> _particleController = GetRequiredServiceLazy<IParticleController>();
    private readonly Lazy<IEquipmentService> _equipmentService = GetRequiredServiceLazy<IEquipmentService>();
    private readonly Lazy<IItemService> _itemService = GetRequiredServiceLazy<IItemService>();
    
    protected override void OnStart()
    {
        _itemService.Value.Initialize();
        _equipmentService.Value.Initialize();
        _itemController.Value.Initialize();
        _particleController.Value.Initialize();
        
        Core.Command.RegisterCommand(
            commandName: "gun",
            handler: GunHandler,
            registerRaw: true
        );
        
        Core.Command.RegisterCommand(
            commandName: "r",
            handler: Register,
            registerRaw: true
        );
        
        Core.Command.RegisterCommand(
            commandName: "d",
            handler: Debug,
            registerRaw: true
        );
    }

    private void Debug(ICommandContext context)
    {
        var player = context.Sender;
        
        if (player == null) return;

        if (!context.IsSentByPlayer) return;

        var equipmentService = (EquipmentService)_equipmentService.Value;

        var weapons = equipmentService.GetAllItems();

        Core.PlayerManager.SendChat($"========== WEAPONS ==========");
        
        foreach (var weapon in weapons)
        {
            Core.PlayerManager.SendChat($"weapon = {weapon.DisplayName}");
        }
    }
    
    private void Register(ICommandContext context)
    {
        var player = context.Sender;
        
        if (player == null) return;

        if (!context.IsSentByPlayer) return;

        var itemService = _itemService.Value;

        var weapons = itemService.GetAllRegisteredItems();

        Core.PlayerManager.SendChat($"========== REGISTER ==========");
        
        foreach (var weapon in weapons)
        {
            Core.PlayerManager.SendChat($"weapon = {weapon.DisplayName}");
        }
    }
    
    private void GunHandler(ICommandContext context)
    {
        var player = context.Sender;
        
        if (player == null) return;

        if (!context.IsSentByPlayer) return;

        var equipmentService = _equipmentService.Value;

        /*equipmentService.GiveWeapon<Omega>(player);
        equipmentService.GiveWeapon<Elite>(player);
        equipmentService.GiveWeapon<ReactorLeak>(player);
        equipmentService.GiveWeapon<Frostbyte>(player);
        equipmentService.GiveWeapon<Blackline>(player);
        equipmentService.GiveWeapon<X3>(player);*/
        equipmentService.GiveGrenade<FrostNade>(player);
        equipmentService.GiveGrenade<FireNade>(player);
    }
}