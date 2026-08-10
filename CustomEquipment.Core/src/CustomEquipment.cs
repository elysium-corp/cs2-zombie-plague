using Common.Di;
using CustomEquipment.Controllers;
using CustomEquipment.Data.Equipments.Weapons.Equipments;
using CustomEquipment.Data.Equipments.Weapons.Grenades;
using CustomEquipment.Data.Equipments.Weapons.Guns;
using CustomEquipment.Di;
using CustomEquipment.Menus;
using CustomEquipment.Registry;
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
    private readonly Lazy<IItemRegistry> _itemService = GetRequiredServiceLazy<IItemRegistry>();
    private readonly Lazy<EquipmentMenu> _equipmentMenu = GetRequiredServiceLazy<EquipmentMenu>();
    private readonly Lazy<IItemRegistry> _itemRegistry = GetRequiredServiceLazy<IItemRegistry>();
    
    protected override void OnStart()
    {
        _itemRegistry.Value.Initialize();
        _itemService.Value.Initialize();
        _equipmentService.Value.Initialize();
        _itemController.Value.Initialize();
        _particleController.Value.Initialize();
        _equipmentMenu.Value.RegisterCommands();
        
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
        
        Core.Command.RegisterCommand(
            commandName: "mine",
            handler: (ICommandContext context) =>
            {
                var laser = new LaserMine();
                laser.OnPurchase(context.Sender);
            },
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

        var items = _itemRegistry.Value.GetDefinitions();

        Core.PlayerManager.SendChat($"========== REGISTER ==========");
        
        foreach (var item in items)
        {
            Core.PlayerManager.SendChat(
                $"{item.DisplayName} — {item.InternalName}"
            );
        }
    }
    
    private void GunHandler(ICommandContext context)
    {
        var player = context.Sender;
        
        if (player == null) return;

        if (!context.IsSentByPlayer) return;

        var equipmentService = _equipmentService.Value;

        equipmentService.GiveWeapon<Omega>(player);
        equipmentService.GiveWeapon<Elite>(player);
        equipmentService.GiveWeapon<ReactorLeak>(player);
        equipmentService.GiveWeapon<Frostbyte>(player);
        equipmentService.GiveWeapon<Blackline>(player);
        equipmentService.GiveWeapon<X3>(player);
        equipmentService.GiveGrenade<BarrierNade>(player);
        equipmentService.GiveGrenade<FrostNade>(player);
        equipmentService.GiveGrenade<FireNade>(player);
    }
}