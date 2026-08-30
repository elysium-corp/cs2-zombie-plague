using System.Text.Json;
using Common.Database.Migrator;
using Common.Di;
using Common.Effects;
using CustomEquipment.Api;
using CustomEquipment.Api.Data;
using CustomEquipment.Controllers;
using CustomEquipment.Data.Equipments.Weapons.Equipments;
using CustomEquipment.Database;
using CustomEquipment.Di;
using CustomEquipment.Menus;
using CustomEquipment.Registry;
using CustomEquipment.Services;
using Economy.Api;
using Menu.Api;
using Menu.Api.Contracts;
using Menu.Api.Extensions;
using Menu.Api.Providers;
using Menu.Api.Results;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using ZombiePlague.Api;
using ZombiePlague.Api.Menus;

namespace CustomEquipment;

[PluginMetadata(
    Id = "CustomEquipment.Core",
    Version = "0.2.0",
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
    private readonly Lazy<IWeaponCatalogRepository> _weaponCatalog = GetRequiredServiceLazy<IWeaponCatalogRepository>();
    private readonly Lazy<DatabaseMigrator<CustomEquipmentDbContext>> _databaseMigrator =
        GetRequiredServiceLazy<DatabaseMigrator<CustomEquipmentDbContext>>();

    private readonly List<Guid> _commandHooks = [];
    private IDisposable? _mainMenuSubscription;
    private IMenuProviderRegistration? _menuProviderRegistration;

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

        _mainMenuSubscription?.Dispose();
        _mainMenuSubscription = menuApi.Extensions.Subscribe(
            menuId: ZombiePlagueMenuIds.Main,
            handler: ExtendMainMenu
        );

        _menuProviderRegistration?.Dispose();
        _menuProviderRegistration = menuApi.RegisterProvider(new MenuProviderDescriptor
        {
            ProviderKey = "custom_equipment",
            DisplayName = "Custom Equipment",
            PluginVersion = "0.2.0",
            Capabilities =
            [
                MenuProviderCapabilityKeys.OpenMenu,
                MenuProviderCapabilityKeys.Actions,
            ],
        });

        if (_menuProviderRegistration.IsRegistered)
        {
            _menuProviderRegistration.RegisterMenu(new MenuProviderMenuDescriptor
            {
                MenuKey = "weapons",
                DisplayName = new LocalizedText
                {
                    Default = "Оружие",
                    Translations = new Dictionary<string, string>
                    {
                        ["ru"] = "Оружие",
                        ["en"] = "Weapons",
                    },
                },
                Handler = context =>
                {
                    _equipmentMenu.Value.Open(context.Target);
                    return MenuOperationResult.Succeeded;
                },
            });

            _menuProviderRegistration.RegisterAction(new MenuProviderActionDescriptor
            {
                ActionKey = "select_weapon",
                DisplayName = new LocalizedText
                {
                    Default = "Выдать оружие",
                    Translations = new Dictionary<string, string>
                    {
                        ["ru"] = "Выдать оружие",
                        ["en"] = "Give weapon",
                    },
                },
                ArgumentsSchema = JsonSerializer.SerializeToElement(new
                {
                    type = "object",
                    additionalProperties = false,
                    required = new[] { "weapon" },
                    properties = new
                    {
                        weapon = new
                        {
                            type = "string",
                            minLength = 1,
                            maxLength = 128,
                        },
                    },
                }),
                Validator = ValidateSelectWeapon,
                Handler = SelectWeapon,
            });
        }
    }

    protected override void OnReady()
    {
        _itemRegistry.Value.Initialize();
        TryReloadDatabaseWeapons(out _);

        _equipmentService.Value.Initialize();
        _itemController.Value.Initialize();
        _equipmentController.Value.Initialize();
        _soundController.Value.Initialize();
        _equipmentMenu.Value.RegisterCommands();

        RegisterCommands();
    }

    protected override void OnUnload()
    {
        EffectService.Release(Core);
        _mainMenuSubscription?.Dispose();
        _mainMenuSubscription = null;
        _menuProviderRegistration?.Dispose();
        _menuProviderRegistration = null;

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
        var localizer = core.Translation.GetPlayerLocalizer(context.Player);
        var option = new ButtonMenuOption(localizer["Menu.Main.Item.Equipment.Title"]);

        option.Click += (_, args) =>
        {
            core.Scheduler.NextTickAsync(() => _equipmentMenu.Value.Open(args.Player));
            return ValueTask.CompletedTask;
        };

        context.Options.Add(option, 3);
    }

    private MenuValidationResult ValidateSelectWeapon(JsonElement arguments)
    {
        if (arguments.ValueKind != JsonValueKind.Object
            || !arguments.TryGetProperty("weapon", out var weaponElement)
            || weaponElement.ValueKind != JsonValueKind.String)
        {
            return MenuValidationResult.Invalid(
                "weapon_required",
                "Аргумент weapon должен быть непустой строкой.",
                "$.weapon");
        }

        var weapon = weaponElement.GetString();
        if (string.IsNullOrWhiteSpace(weapon)
            || weapon.Length > 128
            || !_itemRegistry.Value.GetDefinitions().Any(item =>
                item.InternalName.Equals(weapon, StringComparison.Ordinal)))
        {
            return MenuValidationResult.Invalid(
                "weapon_unknown",
                "Оружие отсутствует в текущем каталоге Provider.",
                "$.weapon");
        }

        return MenuValidationResult.Valid;
    }

    private MenuOperationResult SelectWeapon(MenuProviderInvocationContext context)
    {
        if (!context.Target.IsValid || !context.Target.IsAlive)
        {
            return MenuOperationResult.Failure(
                Menu.Api.Enums.MenuOperationStatus.ValidationFailed,
                "target_not_alive");
        }

        var weapon = context.Arguments.GetProperty("weapon").GetString()!;
        return _equipmentService.Value.TryGiveItem(context.Target, weapon)
            ? MenuOperationResult.Succeeded
            : MenuOperationResult.Failure(
                Menu.Api.Enums.MenuOperationStatus.ValidationFailed,
                "weapon_unavailable");
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

        _equipmentService.Value.TryGiveItem(player, new LaserMine().InternalName);
    }

    private void ReloadHandler(ICommandContext context)
    {
        if (context.IsSentByPlayer)
        {
            context.Reply("This command can only be executed from the server console.");
            return;
        }

        if (TryReloadDatabaseWeapons(out var count))
        {
            context.Reply($"CustomEquipment reloaded: {count} enabled database weapons.");
            return;
        }

        context.Reply("CustomEquipment reload failed; the previous database snapshot is still active.");
    }

    private bool TryReloadDatabaseWeapons(out int count)
    {
        count = 0;

        try
        {
            var weapons = _weaponCatalog.Value.GetEnabledWeapons();
            _itemRegistry.Value.ReplaceDatabaseWeapons(weapons);
            count = weapons.Count;

            Core.Logger.LogInformation("Loaded {WeaponCount} custom equipment weapons from PostgreSQL.", count);
            return true;
        }
        catch (Exception exception)
        {
            Core.Logger.LogError(
                exception,
                "Failed to load custom equipment weapons. The previous database snapshot is still active."
            );
            return false;
        }
    }
}
