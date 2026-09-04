using System.Globalization;
using Admin.Api;
using Common.Database.Migrator;
using Common.Di;
using CustomEquipment.Api;
using Economy.Api;
using Localization.Api;
using Menu.Api;
using Menu.Api.Extensions;
using Microsoft.Extensions.Logging;
using Shop.Api;
using Shop.Core.Api;
using Shop.Core.Application;
using Shop.Core.Data;
using Shop.Core.Database;
using Shop.Core.Di;
using Shop.Core.Menus;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;
using ZombiePlague.Api.Menus;

namespace Shop.Core;

[PluginMetadata(
    Id = "Shop.Core",
    Version = "1.0.0",
    Name = "Elysium Shop",
    Author = "Elysium",
    Description = "Memory-snapshot shop for human and zombie equipment")]
internal sealed class ShopPlugin(ISwiftlyCore core) : Plugin<ShopModule>(core)
{
    private readonly Lazy<ShopApi> _api = GetRequiredServiceLazy<ShopApi>();
    private readonly Lazy<ShopMenu> _menu = GetRequiredServiceLazy<ShopMenu>();
    private readonly Lazy<ShopSnapshotCache> _cache = GetRequiredServiceLazy<ShopSnapshotCache>();
    private readonly Lazy<ShopAccessEvaluator> _access = GetRequiredServiceLazy<ShopAccessEvaluator>();
    private readonly Lazy<ShopPurchaseService> _purchases = GetRequiredServiceLazy<ShopPurchaseService>();
    private readonly Lazy<ShopPurchaseCounter> _counters = GetRequiredServiceLazy<ShopPurchaseCounter>();
    private readonly Lazy<ShopSnapshotCoordinator> _coordinator = GetRequiredServiceLazy<ShopSnapshotCoordinator>();
    private readonly Lazy<ShopAdminApiProxy> _admin = GetRequiredServiceLazy<ShopAdminApiProxy>();
    private readonly Lazy<Func<ILocalizationApi>> _localization =
        GetRequiredServiceLazy<Func<ILocalizationApi>>();
    private readonly Lazy<DatabaseMigrator<ShopDbContext>> _migrator =
        GetRequiredServiceLazy<DatabaseMigrator<ShopDbContext>>();

    private readonly List<Guid> _commands = [];
    private readonly CancellationTokenSource _lifetime = new();
    private readonly object _tasksSync = new();
    private readonly HashSet<Task> _tasks = [];
    private IDisposable? _mainMenuSubscription;
    private Guid _roundStartHook;

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IShopApi, ShopApi>(IShopApi.SharedApiKey, _api.Value);
    }

    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        BindSharedInterface<ICustomEquipmentApi>(interfaceManager, ICustomEquipmentApi.SharedApiKey);
        BindSharedInterface<IEconomyApi>(interfaceManager, IEconomyApi.SharedApiKey);
        BindSharedInterface<ILocalizationApi>(interfaceManager, ILocalizationApi.SharedApiKey);
        BindSharedInterface<IZombiePlagueApi>(interfaceManager, IZombiePlagueApi.SharedApiKey);
    }

    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        if (interfaceManager.TryGetSharedInterface<IAdminApi>(IAdminApi.SharedApiKey, out var adminApi))
        {
            _admin.Value.Initialize(adminApi);
        }
        else
        {
            _admin.Value.Uninitialize();
            Core.Logger.LogWarning(
                "[Shop] Admin.Core не загружен. Офферы с ограничениями по привилегиям закрыты.");
        }

        var menuApi = interfaceManager.GetSharedInterface<IMenuApi>(IMenuApi.SharedApiKey);
        _mainMenuSubscription?.Dispose();
        _mainMenuSubscription = menuApi.Extensions.Subscribe(
            ZombiePlagueMenuIds.Main,
            ExtendMainMenu);
        _menu.Value.RebindExternalEvents();
    }

    protected override void OnStart()
    {
        try
        {
            _migrator.Value.Migrate();
        }
        catch (Exception exception)
        {
            Core.Logger.LogError(
                exception,
                "[Shop] Миграция PostgreSQL не выполнена. Будет использован fallback-конфиг.");
        }

        _coordinator.Value.Start();
    }

    protected override void OnReady()
    {
        _menu.Value.RegisterCommands();
        _menu.Value.Initialize();
        Core.Event.OnClientKeyStateChanged += OnClientKeyStateChanged;
        Core.Event.OnMapUnload += OnMapUnload;
        _roundStartHook = Core.GameEvent.HookPost<EventRoundStart>(OnRoundStart);
        RegisterCommands();
        Core.Logger.LogInformation("[Shop] Shop.Core 1.0.0 загружен.");
    }

    protected override void OnUnload()
    {
        foreach (var command in _commands)
        {
            Core.Command.UnregisterCommand(command);
        }
        _commands.Clear();

        Core.Event.OnClientKeyStateChanged -= OnClientKeyStateChanged;
        Core.Event.OnMapUnload -= OnMapUnload;
        if (_roundStartHook != Guid.Empty)
        {
            Core.GameEvent.Unhook(_roundStartHook);
            _roundStartHook = Guid.Empty;
        }

        _mainMenuSubscription?.Dispose();
        _mainMenuSubscription = null;
        _menu.Value.UnregisterCommands();
        _menu.Value.Dispose();
        _admin.Value.Uninitialize();
        _lifetime.Cancel();
        _coordinator.Value.Dispose();
        DrainTasks();
    }

    protected override void OnStop() => _lifetime.Dispose();

    private void RegisterCommands()
    {
        _commands.Add(Core.Command.RegisterCommand(
            "shop_reload",
            ReloadCommand,
            registerRaw: true,
            permission: "shop.admin",
            helpText: LocalizeForServer("Shop.Commands.Reload.Help")));
        _commands.Add(Core.Command.RegisterCommand(
            "shop_status",
            StatusCommand,
            registerRaw: true,
            permission: "shop.admin",
            helpText: LocalizeForServer("Shop.Commands.Status.Help")));
    }

    private void ExtendMainMenu(MenuExtensionContext context)
    {
        if (context.Player.Controller.Team is not (Team.T or Team.CT))
        {
            return;
        }

        var type = _access.Value.GetShopType(context.Player);
        if (!_cache.Value.Current.Storefronts.TryGetValue(type, out var storefront) || !storefront.Enabled)
        {
            return;
        }

        var option = new ButtonMenuOption(
            _localization.Value().GetForPlayer(context.Player, storefront.TitleKey) ?? storefront.TitleKey);
        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => _menu.Value.Open(args.Player));
            return ValueTask.CompletedTask;
        };
        context.Options.Add(option, 3);
    }

    private void OnClientKeyStateChanged(IOnClientKeyStateChangedEvent @event)
    {
        if (@event.Key != KeyKind.E || !@event.Pressed ||
            Core.PlayerManager.GetPlayer(@event.PlayerId) is not { IsFakeClient: false } player)
        {
            return;
        }

        _purchases.Value.TryPurchaseActiveWeaponAmmo(player);
    }

    private HookResult OnRoundStart(EventRoundStart @event)
    {
        _ = @event;
        _counters.Value.ResetRound();
        _menu.Value.RefreshOpenMenus();
        return HookResult.Continue;
    }

    private void OnMapUnload(IOnMapUnloadEvent @event)
    {
        _ = @event;
        _counters.Value.ResetMap();
        _coordinator.Value.ReloadAtMapEnd();
    }

    private void ReloadCommand(ICommandContext context)
    {
        var playerId = context.Sender?.PlayerID;
        context.Reply(LocalizeForContext(context, "Shop.Admin.Reload.Started"));
        Track(ReloadAsync(playerId));
    }

    private async Task ReloadAsync(int? playerId)
    {
        var succeeded = await _coordinator.Value.ReloadNowAsync().ConfigureAwait(false);
        if (_lifetime.IsCancellationRequested)
        {
            return;
        }

        Core.Scheduler.NextTick(() =>
        {
            if (_lifetime.IsCancellationRequested)
            {
                return;
            }

            if (succeeded)
            {
                _menu.Value.RefreshOpenMenus();
            }

            if (playerId is { } id)
            {
                if (Core.PlayerManager.GetPlayer(id) is not { IsValid: true } player)
                {
                    return;
                }

                var key = succeeded
                    ? "Shop.Admin.Reload.Succeeded"
                    : "Shop.Admin.Reload.Failed";
                var message = succeeded
                    ? _localization.Value().FormatForPlayer(
                        player,
                        key,
                        new Dictionary<string, object?>
                        {
                            ["source"] = _cache.Value.Current.Source,
                            ["offers"] = _cache.Value.Current.Offers.Count
                        })
                    : _localization.Value().GetForPlayer(player, key);
                player.SendChat(message ?? key);
            }
            else
            {
                if (succeeded)
                {
                    Core.Logger.LogInformation(
                        "[Shop] Snapshot обновлён: {Source}, офферов: {OfferCount}.",
                        _cache.Value.Current.Source,
                        _cache.Value.Current.Offers.Count);
                }
                else
                {
                    Core.Logger.LogWarning(
                        "[Shop] Snapshot не обновлён; сохранено предыдущее состояние.");
                }
            }
        });
    }

    private void StatusCommand(ICommandContext context)
    {
        var snapshot = _cache.Value.Current;
        context.Reply(FormatForContext(
            context,
            "Shop.Admin.Status",
            new Dictionary<string, object?>
            {
                ["source"] = snapshot.Source,
                ["categories"] = snapshot.Categories.Count,
                ["offers"] = snapshot.Offers.Count,
                ["loaded"] = snapshot.LoadedAt.ToString("O", CultureInfo.InvariantCulture)
            }));
    }

    private string LocalizeForContext(ICommandContext context, string key)
    {
        return context.Sender is { } player
            ? _localization.Value().GetForPlayer(player, key) ?? key
            : LocalizeForServer(key);
    }

    private string FormatForContext(
        ICommandContext context,
        string key,
        IReadOnlyDictionary<string, object?> parameters)
    {
        var localized = context.Sender is { } player
            ? _localization.Value().FormatForPlayer(player, key, parameters)
            : _localization.Value().FormatForLanguage(
                _localization.Value().ServerFallbackLanguage,
                key,
                parameters);
        return localized ?? key;
    }

    private string LocalizeForServer(string key) =>
        _localization.Value().GetForLanguage(
            _localization.Value().ServerFallbackLanguage,
            key) ?? key;

    private void Track(Task task)
    {
        lock (_tasksSync)
        {
            _tasks.Add(task);
        }

        _ = task.ContinueWith(
            completed =>
            {
                lock (_tasksSync)
                {
                    _tasks.Remove(completed);
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private void DrainTasks()
    {
        Task[] tasks;
        lock (_tasksSync)
        {
            tasks = _tasks.ToArray();
        }

        try
        {
            Task.WhenAll(tasks).Wait(TimeSpan.FromSeconds(10));
        }
        catch (AggregateException exception) when (
            exception.InnerExceptions.All(inner => inner is OperationCanceledException))
        {
        }
    }
}
