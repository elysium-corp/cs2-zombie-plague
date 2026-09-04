using Admin.Api;
using Advertisement.Api;
using Advertisement.Core.Application;
using Advertisement.Core.Api;
using Advertisement.Core.Data;
using Advertisement.Core.Database;
using Advertisement.Core.Di;
using Common.Database.Migrator;
using Common.Di;
using Localization.Api;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Plugins;

namespace Advertisement.Core;

[PluginMetadata(
    Id = "Advertisement.Core",
    Version = "2.4.0",
    Name = "Elysium Advertisements",
    Author = "Elysium",
    Description = "Реклама Elysium с общей локализацией через Localization.Core.")]
internal sealed class AdvertisementPlugin(ISwiftlyCore core) : Plugin<AdvertisementModule>(core)
{
    private readonly List<Guid> _commands = [];
    private readonly CancellationTokenSource _lifetime = new();
    private readonly HashSet<Task> _pendingOperations = [];
    private readonly object _pendingSync = new();
    private readonly Lazy<AdvertisementCache> _cache = GetRequiredServiceLazy<AdvertisementCache>();
    private readonly Lazy<AdvertisementCoordinator> _coordinator = GetRequiredServiceLazy<AdvertisementCoordinator>();
    private readonly Lazy<AdvertisementScheduler> _scheduler = GetRequiredServiceLazy<AdvertisementScheduler>();
    private readonly Lazy<AdvertisementApi> _api = GetRequiredServiceLazy<AdvertisementApi>();
    private readonly Lazy<AdminAudienceResolver> _audienceResolver = GetRequiredServiceLazy<AdminAudienceResolver>();
    private readonly Lazy<DatabaseMigrator<AdvertisementDbContext>> _databaseMigrator =
        GetRequiredServiceLazy<DatabaseMigrator<AdvertisementDbContext>>();

    private CancellationTokenSource? _schedulerTimer;
    private string _currentMapName = string.Empty;

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IAdvertisementApi, AdvertisementApi>(
            IAdvertisementApi.SharedApiKey,
            _api.Value);
    }

    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        BindSharedInterface<ILocalizationApi>(interfaceManager, ILocalizationApi.SharedApiKey);
    }

    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        if (interfaceManager.TryGetSharedInterface<IAdminApi>(IAdminApi.SharedApiKey, out var adminApi))
        {
            _audienceResolver.Value.Initialize(adminApi);
        }
        else
        {
            _audienceResolver.Value.Uninitialize();
            Core.Logger.LogWarning(
                "[Advertisement] Admin.Core не загружен. Аудитории admin_group будут пропущены.");
        }
    }

    protected override void OnStart()
    {
        TryMigrateDatabase();
        Core.Event.OnMapLoad += OnMapLoad;
        RegisterCommands();
        _coordinator.Value.Start();
    }

    protected override void OnReady()
    {
        _scheduler.Value.TryStartFromCurrentMap();
        _currentMapName = _scheduler.Value.CurrentMapName;
        _schedulerTimer = Core.Scheduler.RepeatBySeconds(1f, _scheduler.Value.Tick);
        Core.Logger.LogInformation("[Advertisement] Advertisement.Core 2.4.0 загружен.");
    }

    protected override void OnUnload()
    {
        foreach (var command in _commands)
        {
            Core.Command.UnregisterCommand(command);
        }
        _commands.Clear();

        _schedulerTimer?.Cancel();
        _schedulerTimer = null;
        Core.Event.OnMapLoad -= OnMapLoad;

        _lifetime.Cancel();
        _audienceResolver.Value.Uninitialize();
        _coordinator.Value.Dispose();
        DrainPendingOperations();
    }

    protected override void OnStop()
    {
        _lifetime.Dispose();
    }

    private void TryMigrateDatabase()
    {
        try
        {
            _databaseMigrator.Value.Migrate();
        }
        catch (Exception exception)
        {
            Core.Logger.LogError(
                exception,
                "[Advertisement] Миграция PostgreSQL не выполнена. Плагин продолжит работу с fallback-конфигурацией.");
        }
    }

    private void RegisterCommands()
    {
        _commands.Add(Core.Command.RegisterCommand(
            "ads_status",
            StatusCommand,
            registerRaw: true,
            permission: "advertisement.admin",
            helpText: "Показать состояние Advertisement.Core"));
        _commands.Add(Core.Command.RegisterCommand(
            "ads_reload",
            ReloadCommand,
            registerRaw: true,
            permission: "advertisement.admin",
            helpText: "Перезагрузить рекламу из PostgreSQL"));
        _commands.Add(Core.Command.RegisterCommand(
            "ads_test",
            TestCommand,
            registerRaw: true,
            permission: "advertisement.admin",
            helpText: "Показать сообщение: ads_test <key> [language]"));
    }

    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        var previousMap = _currentMapName;
        _currentMapName = @event.MapName;
        _scheduler.Value.OnMapLoaded(@event.MapName);

        if (!string.IsNullOrWhiteSpace(previousMap)
            && !string.Equals(previousMap, @event.MapName, StringComparison.OrdinalIgnoreCase))
        {
            _ = _coordinator.Value.ReloadForMapAsync(@event.MapName);
        }
    }

    private void StatusCommand(ICommandContext context)
    {
        var snapshot = _cache.Value.Current;
        if (snapshot is null)
        {
            context.Reply("Advertisement.Core 2.4.0\nSnapshot: загружается");
            return;
        }

        var players = Core.PlayerManager.GetAllPlayers().ToArray();
        var bots = players.Count(player => player.IsFakeClient);
        var count = snapshot.Settings.ExcludeBotsFromPlayers ? players.Length - bots : players.Length;
        context.Reply(
            $"Advertisement.Core 2.4.0\nSource: {snapshot.Source}\nMessages: {snapshot.Messages.Count}" +
            $"\nActive: {snapshot.ActiveMessageCount(DateTimeOffset.UtcNow, count)}" +
            $"\nLocalization: Localization.Core\nVersion: {snapshot.Settings.ConfigurationVersion}");
    }

    private void ReloadCommand(ICommandContext context)
    {
        var playerId = context.Sender?.PlayerID;
        context.Reply("Advertisement reload started.");
        Track(ReloadAsync(playerId));
    }

    private async Task ReloadAsync(int? playerId)
    {
        var result = await _coordinator.Value.ReloadNowAsync();
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

            if (playerId is { } id)
            {
                Core.PlayerManager.GetPlayer(id)?.SendChat($"[Advertisement] {result.Message}");
            }
            else
            {
                Core.Logger.LogInformation("[Advertisement] {Result}", result.Message);
            }
        });
    }

    private void TestCommand(ICommandContext context)
    {
        if (context.Sender is null || context.Args.Length == 0)
        {
            context.Reply("Использование: ads_test <key> [language]");
            return;
        }

        var message = _cache.Value.Current?.Messages.Values.FirstOrDefault(value =>
            value.Key.Equals(context.Args[0], StringComparison.OrdinalIgnoreCase));
        if (message is null)
        {
            context.Reply($"Сообщение '{context.Args[0]}' не найдено.");
            return;
        }

        _scheduler.Value.SendTest(
            message,
            context.Sender,
            context.Args.Length > 1 ? context.Args[1] : null);
    }

    private void Track(Task task)
    {
        lock (_pendingSync)
        {
            _pendingOperations.Add(task);
        }

        _ = task.ContinueWith(
            completed =>
            {
                lock (_pendingSync)
                {
                    _pendingOperations.Remove(completed);
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private void DrainPendingOperations()
    {
        Task[] tasks;
        lock (_pendingSync)
        {
            tasks = _pendingOperations.ToArray();
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
