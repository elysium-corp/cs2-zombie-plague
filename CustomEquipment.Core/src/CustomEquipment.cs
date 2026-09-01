using Common.Database.Migrator;
using Common.Di;
using Common.Effects;
using CustomEquipment.Api;
using CustomEquipment.Api.Data;
using CustomEquipment.Controllers;
using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Data.Equipments.Weapons.Equipments;
using CustomEquipment.Database;
using CustomEquipment.Di;
using CustomEquipment.Menus;
using CustomEquipment.Registry;
using CustomEquipment.Services;
using Economy.Api;
using Menu.Api;
using Menu.Api.Extensions;
using Localization.Api;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using ZombiePlague.Api;
using ZombiePlague.Api.Menus;

namespace CustomEquipment;

[PluginMetadata(
    Id = "CustomEquipment.Core",
    Version = "0.3.0",
    Name = "[ZP] CustomEquipment",
    Author = "illusion & fdrinv",
    Description = "Database-backed custom equipment and weapon sounds"
)]
internal sealed partial class CustomEquipment(ISwiftlyCore core) : Plugin<CustomEquipmentModule>(core)
{
    private readonly Lazy<IWeaponController> _itemController = GetRequiredServiceLazy<IWeaponController>();
    private readonly Lazy<IWeaponSoundController> _soundController = GetRequiredServiceLazy<IWeaponSoundController>();
    private readonly Lazy<IEquipmentService> _equipmentService = GetRequiredServiceLazy<IEquipmentService>();
    private readonly Lazy<IMineController> _equipmentController = GetRequiredServiceLazy<IMineController>();
    private readonly Lazy<CustomEquipmentApi> _customEquipmentApi = GetRequiredServiceLazy<CustomEquipmentApi>();
    private readonly Lazy<EquipmentMenu> _equipmentMenu = GetRequiredServiceLazy<EquipmentMenu>();
    private readonly Lazy<IItemRegistry> _itemRegistry = GetRequiredServiceLazy<IItemRegistry>();
    private readonly Lazy<EquipmentCatalogSynchronizer> _catalogSynchronizer =
        GetRequiredServiceLazy<EquipmentCatalogSynchronizer>();
    private readonly Lazy<ILocalizationApi> _localization = GetRequiredServiceLazy<ILocalizationApi>();
    private readonly Lazy<DatabaseMigrator<CustomEquipmentDbContext>> _databaseMigrator =
        GetRequiredServiceLazy<DatabaseMigrator<CustomEquipmentDbContext>>();

    private readonly List<Guid> _commandHooks = [];
    private IDisposable? _mainMenuSubscription;

    protected override void OnStart()
    {
        try
        {
            _databaseMigrator.Value.Migrate();
        }
        catch (Exception exception)
        {
            Core.Logger.LogError(
                exception,
                "Custom equipment database migration failed. Compiled equipment will remain available."
            );
        }
    }

    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        BindSharedInterface<IEconomyApi>(interfaceManager, IEconomyApi.SharedApiKey);
        BindSharedInterface<IZombiePlagueApi>(interfaceManager, IZombiePlagueApi.SharedApiKey);
        BindSharedInterface<ILocalizationApi>(interfaceManager, ILocalizationApi.SharedApiKey);
    }

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<ICustomEquipmentApi, CustomEquipmentApi>(
            ICustomEquipmentApi.SharedApiKey,
            _customEquipmentApi.Value
        );
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
        _catalogSynchronizer.Value.TryReload(out _, out _);
        _catalogSynchronizer.Value.Start();

        _equipmentService.Value.Initialize();
        _itemController.Value.Initialize();
        _equipmentController.Value.Initialize();
        _soundController.Value.Initialize();
        _equipmentMenu.Value.RegisterCommands();

        RegisterCommands();
    }

    protected override void OnUnload()
    {
        _catalogSynchronizer.Value.Stop();
        EffectService.Release(Core);
        _mainMenuSubscription?.Dispose();
        _mainMenuSubscription = null;

        _equipmentMenu.Value.UnregisterCommands();

        foreach (var hook in _commandHooks)
        {
            Core.Command.UnregisterCommand(hook);
        }

        _commandHooks.Clear();
    }

    private void RegisterCommands()
    {
        _commandHooks.Add(Core.Command.RegisterCommand(
            commandName: "gun",
            handler: GunHandler,
            registerRaw: true
        ));

        _commandHooks.Add(Core.Command.RegisterCommand(
            commandName: "mine",
            handler: MineHandler,
            registerRaw: true
        ));

        _commandHooks.Add(Core.Command.RegisterCommand(
            commandName: "custom_equipment_reload",
            handler: ReloadHandler,
            registerRaw: true,
            helpText: "Reload CustomEquipment weapons and sounds from PostgreSQL"
        ));
    }

    private void ExtendMainMenu(MenuExtensionContext context)
    {
        var title = _localization.Value.GetForPlayer(context.Player, "Menu.Main.Item.Equipment.Title")
                    ?? "Equipment Shop";
        var option = new ButtonMenuOption(title);

        option.Click += (_, args) =>
        {
            core.Scheduler.NextTickAsync(() => _equipmentMenu.Value.Open(args.Player));
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

        var items = _itemRegistry.Value
            .GetDefinitions()
            .Where(item => item is WeaponItemBase or GrenadeItemBase);

        foreach (var item in items)
        {
            _equipmentService.Value.TryGiveItem(player, item.InternalName);
        }
    }

    private void MineHandler(ICommandContext context)
    {
        var player = context.Sender;

        if (!context.IsSentByPlayer || player is null)
        {
            return;
        }

        _equipmentService.Value.TryGiveItem(
            player,
            GameplayItemDefaults.Get(GameplayItemKeys.LaserMine).InternalName
        );
    }

    private void ReloadHandler(ICommandContext context)
    {
        if (context.IsSentByPlayer)
        {
            context.Reply("This command can only be executed from the server console.");
            return;
        }

        if (_catalogSynchronizer.Value.TryReload(out var weaponCount, out var gameplayItemCount))
        {
            context.Reply(
                $"CustomEquipment reloaded: {weaponCount} database weapons, " +
                $"{gameplayItemCount} enabled grenades/equipment items."
            );
            return;
        }

        context.Reply("CustomEquipment reload failed; the previous database snapshot is still active.");
    }

}
