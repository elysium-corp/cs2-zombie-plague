using Common.Di;
using CustomEquipment.Api;
using CustomEquipment.Api.Data;
using CustomEquipment.Controllers;
using CustomEquipment.Data.Equipments.Weapons.Equipments;
using CustomEquipment.Di;
using CustomEquipment.Menus;
using CustomEquipment.Registry;
using CustomEquipment.Services;
using Economy.Api;
using Menu.Api;
using Menu.Api.Extensions;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using ZombiePlague.Api;
using ZombiePlague.Api.Menus;

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
    private readonly Lazy<CustomEquipmentApi> _customEquipmentApi = GetRequiredServiceLazy<CustomEquipmentApi>();
    private readonly Lazy<EquipmentMenu> _equipmentMenu = GetRequiredServiceLazy<EquipmentMenu>();
    private readonly Lazy<IItemRegistry> _itemRegistry = GetRequiredServiceLazy<IItemRegistry>();
    
    private IDisposable? _mainMenuSubscription;
    
    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        BindSharedInterface<IEconomyApi>(interfaceManager, IEconomyApi.SharedApiKey);
        BindSharedInterface<IZombiePlagueApi>(interfaceManager, IZombiePlagueApi.SharedApiKey);
    }
    
    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<ICustomEquipmentApi, CustomEquipmentApi>(ICustomEquipmentApi.SharedApiKey, _customEquipmentApi.Value);
    }
    
    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        var menuApi = interfaceManager.GetSharedInterface<IMenuApi>(IMenuApi.SharedApiKey);

        _mainMenuSubscription = menuApi.Extensions.Subscribe(
            menuId: ZombiePlagueMenuIds.Main,
            handler: ExtendMainMenu
        );
    }
    
    protected override void OnReady()
    {
        _itemRegistry.Value.Initialize();
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
            commandName: "mine",
            handler: (ICommandContext context) =>
            {
                var laser = new LaserMine();
                laser.OnPurchase(context.Sender);
            },
            registerRaw: true
        );
    }
    
    protected override void OnUnload()
    {
        _mainMenuSubscription?.Dispose();
        _mainMenuSubscription = null;

        _equipmentMenu.Value.UnregisterCommands();
    }
    
    private void ExtendMainMenu(MenuExtensionContext context)
    {
        var localizer = core.Translation.GetPlayerLocalizer(context.Player);
        var option = new ButtonMenuOption(localizer["Menu.Main.Item.Equipment.Title"]);

        option.Click += (_, args) =>
        {
            core.Scheduler.NextTickAsync(
                () => _equipmentMenu.Value.Open(args.Player)
            );

            return ValueTask.CompletedTask;
        };

        context.Options.Add(option, 3);
    }
    
    private void GunHandler(ICommandContext context)
    {
        var player = context.Sender;

        if (player is null || !context.IsSentByPlayer)
        {
            return;
        }

        var equipmentService = _equipmentService.Value;

        var items = _itemRegistry.Value
            .GetDefinitions()
            .Where(item => item is WeaponItemBase or GrenadeItemBase);

        foreach (var item in items)
        {
            equipmentService.GiveItem(player, item.InternalName);
        }
    }
}