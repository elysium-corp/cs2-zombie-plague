using Admin.Api;
using Common.Database.Migrator;
using Common.Di;
using Menu.Api;
using Menu.Core.Api;
using Menu.Core.Access;
using Menu.Core.Application;
using Menu.Core.Commands;
using Menu.Core.Database;
using Menu.Core.Di;
using Menu.Core.Providers;
using Menu.Core.Runtime;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;

namespace Menu.Core;

[PluginMetadata(
    Id = "Menu.Core", 
    Version = "1.0.0",
    Name = "Elysium Menu.Core",
    Author = "Elysium",
    Description = "Централизованные атомарные меню CS2 с Provider API и Flute CMS."
)]
internal sealed partial class Menu(ISwiftlyCore core) : Plugin<MenuModule>(core)
{
    private readonly Lazy<MenuApi> _menuApi = GetRequiredServiceLazy<MenuApi>();
    private readonly Lazy<AdminAccessResolver> _accessResolver = GetRequiredServiceLazy<AdminAccessResolver>();
    private readonly Lazy<MenuSyncCoordinator> _coordinator = GetRequiredServiceLazy<MenuSyncCoordinator>();
    private readonly Lazy<MenuCommandRouter> _commandRouter = GetRequiredServiceLazy<MenuCommandRouter>();
    private readonly Lazy<MenuSnapshotStore> _snapshots = GetRequiredServiceLazy<MenuSnapshotStore>();
    private readonly Lazy<ProviderRegistry> _providers = GetRequiredServiceLazy<ProviderRegistry>();
    private readonly Lazy<DatabaseMigrator<MenuDbContext>> _databaseMigrator =
        GetRequiredServiceLazy<DatabaseMigrator<MenuDbContext>>();
    private readonly List<Guid> _managementCommands = [];
    private int _unloading;

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IMenuApi, MenuApi>(IMenuApi.SharedApiKey, _menuApi.Value);
    }

    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        if (interfaceManager.TryGetSharedInterface<IAdminApi>(IAdminApi.SharedApiKey, out var adminApi))
        {
            _accessResolver.Value.Bind(adminApi);
        }
        else
        {
            _accessResolver.Value.Bind(null);
            Core.Logger.LogWarning(
                "[Menu] Admin.Core недоступен: public menus работают, protected access запрещён.");
        }
    }

    protected override void OnStart()
    {
        TryMigrateDatabase();
        RegisterManagementCommands();
    }

    protected override void OnReady()
    {
        _commandRouter.Value.Start();
        _coordinator.Value.Start();
        Core.Logger.LogInformation("[Menu] Menu.Core 1.0.0 запущен.");
    }

    protected override void OnUnload()
    {
        Interlocked.Exchange(ref _unloading, 1);
        _commandRouter.Value.Stop();
        foreach (var command in _managementCommands)
        {
            try
            {
                Core.Command.UnregisterCommand(command);
            }
            catch (Exception exception)
            {
                Core.Logger.LogError(
                    exception,
                    "[Menu] Не удалось удалить management command {CommandId} при остановке.",
                    command);
            }
        }

        _managementCommands.Clear();

        try
        {
            _coordinator.Value.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            Core.Logger.LogWarning(exception, "[Menu] Ошибка bounded shutdown sync coordinator.");
        }

        _providers.Value.Stop();
        _accessResolver.Value.Bind(null);
    }

    private void TryMigrateDatabase()
    {
        try
        {
            _databaseMigrator.Value.Migrate();
        }
        catch (Exception exception)
        {
            Core.Logger.LogError(exception,
                "[Menu] Миграция PostgreSQL не выполнена; будет использован LKG/fallback.");
        }
    }

    private void RegisterManagementCommands()
    {
        _managementCommands.Add(Core.Command.RegisterCommand(
            "menu_status",
            StatusCommand,
            registerRaw: false,
            permission: "menu.admin",
            helpText: "Показать состояние Menu.Core"));
        _managementCommands.Add(Core.Command.RegisterCommand(
            "menu_reload",
            ReloadCommand,
            registerRaw: false,
            permission: "menu.admin",
            helpText: "Перезагрузить active Menu Release"));
        _managementCommands.Add(Core.Command.RegisterCommand(
            "menu_validate",
            ValidateCommand,
            registerRaw: false,
            permission: "menu.admin",
            helpText: "Показать результат последней validation"));
    }

    private void StatusCommand(ICommandContext context)
    {
        var snapshot = _snapshots.Value.Current;
        var status = _snapshots.Value.Status;
        context.Reply(
            $"Menu.Core 1.0.0\n" +
            $"Release: {snapshot.ReleaseId}\n" +
            $"Source: {snapshot.Source}\n" +
            $"Menus: {snapshot.Menus.Count}\n" +
            $"Commands: {snapshot.Commands.Count}\n" +
            $"Checksum: {(string.IsNullOrEmpty(snapshot.Checksum) ? "-" : snapshot.Checksum)}\n" +
            $"Last validation: {(status.LastAttemptSucceeded ? "valid" : "degraded")}");
    }

    private void ReloadCommand(ICommandContext context)
    {
        if (Volatile.Read(ref _unloading) != 0)
        {
            context.Reply("[Menu] Плагин уже выгружается.");
            return;
        }

        var playerId = context.Sender?.PlayerID;
        context.Reply("[Menu] Reload started.");
        _ = ReloadAsync(playerId);
    }

    private async Task ReloadAsync(int? playerId)
    {
        MenuReloadResult? result = null;
        Exception? failure = null;
        try
        {
            result = await _coordinator.Value.ReloadNowAsync().ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            failure = exception;
            Core.Logger.LogWarning(exception, "[Menu] Ручная перезагрузка Release завершилась ошибкой.");
        }

        if (Volatile.Read(ref _unloading) != 0)
        {
            return;
        }

        Core.Scheduler.NextTick(() =>
        {
            var message = result is not null
                ? $"[Menu] {result.Message}; release={result.ActiveReleaseId}; source={result.Source}."
                : $"[Menu] Reload failed: {failure?.GetType().Name ?? "unknown_error"}.";
            if (playerId is { } id)
            {
                Core.PlayerManager.GetPlayer(id)?.SendChat(message);
            }
            else
            {
                Core.Logger.LogInformation("{Message}", message);
            }
        });
    }

    private void ValidateCommand(ICommandContext context)
    {
        var status = _snapshots.Value.Status;
        if (status.LastAttemptDiagnostics.Length == 0)
        {
            context.Reply(status.LastAttemptSucceeded
                ? "[Menu] Active Release validation: valid."
                : "[Menu] Release ещё не загружен.");
            return;
        }

        var summary = string.Join("\n", status.LastAttemptDiagnostics
            .Take(10)
            .Select(item => $"{item.Severity}: {item.Code} ({item.Path ?? "$"})"));
        context.Reply($"[Menu] Validation diagnostics:\n{summary}");
    }
}
