using Common.Database.Migrator;
using Common.Di;
using Localization.Api;
using Localization.Core.Api;
using Localization.Core.Application;
using Localization.Core.Database;
using Localization.Core.Di;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;

namespace Localization.Core;

[PluginMetadata(
    Id = "Localization.Core",
    Version = "1.4.0",
    Name = "Elysium Localization",
    Author = "Elysium",
    Description = "Единая локализация Elysium с языком игрока, PostgreSQL и fallback-конфигурацией.")]
internal sealed class LocalizationPlugin(ISwiftlyCore core) : Plugin<LocalizationModule>(core)
{
    private readonly List<Guid> _commands = [];
    private readonly ConcurrentDictionary<int, ulong> _slots = new();
    private readonly HashSet<Task> _pendingOperations = [];
    private readonly object _pendingSync = new();
    private readonly CancellationTokenSource _lifetime = new();

    private readonly Lazy<LocalizationCache> _cache = GetRequiredServiceLazy<LocalizationCache>();
    private readonly Lazy<PlayerLanguageCache> _playerLanguages = GetRequiredServiceLazy<PlayerLanguageCache>();
    private readonly Lazy<PlayerLanguagePreferenceRepository> _preferences =
        GetRequiredServiceLazy<PlayerLanguagePreferenceRepository>();
    private readonly Lazy<PlayerLanguageSelectionService> _selection =
        GetRequiredServiceLazy<PlayerLanguageSelectionService>();
    private readonly Lazy<LocalizationCoordinator> _coordinator = GetRequiredServiceLazy<LocalizationCoordinator>();
    private readonly Lazy<LocalizationApi> _api = GetRequiredServiceLazy<LocalizationApi>();
    private readonly Lazy<LanguageResolver> _languageResolver = GetRequiredServiceLazy<LanguageResolver>();
    private readonly Lazy<RateLimitedLocalizationLogger> _rateLimitedLogger =
        GetRequiredServiceLazy<RateLimitedLocalizationLogger>();
    private readonly Lazy<DatabaseMigrator<LocalizationDbContext>> _databaseMigrator =
        GetRequiredServiceLazy<DatabaseMigrator<LocalizationDbContext>>();

    private Guid? _chatHook;

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<ILocalizationApi, LocalizationApi>(
            ILocalizationApi.SharedApiKey,
            _api.Value);
        interfaceManager.AddSharedInterface<ILanguageResolver, LanguageResolver>(
            ILanguageResolver.SharedApiKey,
            _languageResolver.Value);
    }

    protected override void OnStart()
    {
        TryMigrateDatabase();
        Core.Event.OnClientSteamAuthorize += OnClientSteamAuthorize;
        Core.Event.OnClientDisconnected += OnClientDisconnected;
        Core.Event.OnMapUnload += OnMapUnload;
        RegisterCommands();
        _coordinator.Value.Start();
    }

    protected override void OnReady()
    {
        foreach (var player in Core.PlayerManager.GetAllPlayers().Where(player => player.IsAuthorized))
        {
            BindAndLoad(player.PlayerID, player.SteamID);
        }

        Core.Logger.LogInformation("[Localization] Localization.Core 1.4.0 загружен.");
    }

    protected override void OnUnload()
    {
        if (_chatHook is { } chatHook)
        {
            Core.Command.UnhookClientChat(chatHook);
            _chatHook = null;
        }

        foreach (var command in _commands)
        {
            Core.Command.UnregisterCommand(command);
        }
        _commands.Clear();

        Core.Event.OnClientSteamAuthorize -= OnClientSteamAuthorize;
        Core.Event.OnClientDisconnected -= OnClientDisconnected;
        Core.Event.OnMapUnload -= OnMapUnload;

        _lifetime.Cancel();
        _coordinator.Value.Dispose();
        DrainPendingOperations();
        _slots.Clear();
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
                "[Localization] Миграция PostgreSQL не выполнена. Используется fallback-конфигурация.");
        }
    }

    private void RegisterCommands()
    {
        _commands.Add(Core.Command.RegisterCommand(
            "localization_status",
            StatusCommand,
            registerRaw: true,
            permission: "localization.admin",
            helpText: "Показать состояние Localization.Core"));
        _commands.Add(Core.Command.RegisterCommand(
            "localization_reload",
            ReloadCommand,
            registerRaw: true,
            permission: "localization.admin",
            helpText: "Перезагрузить локализацию из PostgreSQL или fallback-конфига"));
        _commands.Add(Core.Command.RegisterCommand(
            "lang",
            LanguageCommand,
            registerRaw: false,
            helpText: "Выбрать язык"));
        Core.Command.RegisterCommandAlias("lang", "language");
        Core.Command.RegisterCommandAlias("lang", "язык");
        _chatHook = Core.Command.HookClientChat(OnClientChat);
    }

    private void OnClientSteamAuthorize(IOnClientSteamAuthorizeEvent @event)
    {
        var player = Core.PlayerManager.GetPlayer(@event.PlayerId);
        if (player is { IsAuthorized: true })
        {
            BindAndLoad(@event.PlayerId, player.SteamID);
        }
    }

    private void OnClientDisconnected(IOnClientDisconnectedEvent @event)
    {
        if (_slots.TryRemove(@event.PlayerId, out var steamId))
        {
            _playerLanguages.Value.Remove(steamId);
        }
    }

    private void OnMapUnload(IOnMapUnloadEvent _)
    {
        _coordinator.Value.OnMapEnded();
    }

    private HookResult OnClientChat(int playerId, string text, bool teamOnly)
    {
        _ = teamOnly;
        var command = text.Trim();
        if (!command.Equals("!lang", StringComparison.OrdinalIgnoreCase)
            && !command.Equals("!language", StringComparison.OrdinalIgnoreCase)
            && !command.Equals("!язык", StringComparison.OrdinalIgnoreCase))
        {
            return HookResult.Continue;
        }

        var player = Core.PlayerManager.GetPlayer(playerId);
        if (player is not null)
        {
            OpenLanguageMenu(player);
        }

        return HookResult.Handled;
    }

    private void LanguageCommand(ICommandContext context)
    {
        if (context.Sender is null)
        {
            context.Reply("Команда доступна только игроку.");
            return;
        }

        OpenLanguageMenu(context.Sender);
    }

    private void OpenLanguageMenu(IPlayer player)
    {
        var languages = _api.Value.GetEnabledLanguages();
        if (languages.Count == 0)
        {
            var loadingMessage = _api.Value.GetForPlayer(player, "localization.menu.loading");
            if (!string.IsNullOrWhiteSpace(loadingMessage))
            {
                player.SendChat(loadingMessage);
            }

            return;
        }

        var title = _api.Value.GetForPlayer(player, "localization.menu.title");
        if (string.IsNullOrWhiteSpace(title))
        {
            return;
        }

        var builder = Core.MenusAPI.CreateBuilder()
            .EnableExit()
            .SetPlayerFrozen(false)
            .Design.SetMenuTitle(title);

        foreach (var language in languages)
        {
            var button = new ButtonMenuOption(language.NativeName) { CloseAfterClick = true };
            var playerId = player.PlayerID;
            var steamId = player.SteamID;
            button.Click += (_, _) =>
            {
                Track(SaveLanguageAsync(playerId, steamId, language));
                return ValueTask.CompletedTask;
            };
            builder.AddOption(button);
        }

        Core.MenusAPI.OpenMenuForPlayer(player, builder.Build());
    }

    private async Task SaveLanguageAsync(
        int playerId,
        ulong steamId,
        LocalizationLanguage language)
    {
        try
        {
            await _selection.Value.SetAsync(steamId, language.Code, _lifetime.Token);
            if (_lifetime.IsCancellationRequested)
            {
                return;
            }

            var message = _api.Value.GetForLanguage(
                              language.Code,
                              "localization.menu.changed",
                              new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                              {
                                  ["language"] = language.NativeName,
                              });
            Core.Scheduler.NextTick(() =>
            {
                if (_lifetime.IsCancellationRequested)
                {
                    return;
                }

                var current = Core.PlayerManager.GetPlayer(playerId);
                if (current?.SteamID == steamId && !string.IsNullOrWhiteSpace(message))
                {
                    current.SendChat(message);
                }
            });
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (_lifetime.IsCancellationRequested)
            {
                return;
            }

            _rateLimitedLogger.Value.Warning(
                $"preference:save:{steamId}",
                TimeSpan.FromMinutes(2),
                "[Localization] Не удалось сохранить язык игрока {SteamId}: {Error}",
                steamId,
                exception.Message);
            Core.Scheduler.NextTick(() =>
            {
                if (_lifetime.IsCancellationRequested)
                {
                    return;
                }

                var current = Core.PlayerManager.GetPlayer(playerId);
                if (current?.SteamID == steamId)
                {
                    var unavailableMessage = _api.Value.GetForPlayer(
                        current,
                        "localization.menu.unavailable");
                    if (!string.IsNullOrWhiteSpace(unavailableMessage))
                    {
                        current.SendChat(unavailableMessage);
                    }
                }
            });
        }
    }

    private void BindAndLoad(int playerId, ulong steamId)
    {
        _slots[playerId] = steamId;
        var generation = _playerLanguages.Value.BeginLoad(steamId);
        Track(LoadPreferenceAsync(steamId, generation));
    }

    private async Task LoadPreferenceAsync(ulong steamId, long generation)
    {
        try
        {
            var language = await _preferences.Value.LoadAsync(steamId, _lifetime.Token);
            if (_lifetime.IsCancellationRequested)
            {
                return;
            }

            _playerLanguages.Value.CompleteLoad(steamId, generation, language);
        }
        catch (OperationCanceledException) when (_lifetime.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            _playerLanguages.Value.CompleteLoad(steamId, generation, null);
            _rateLimitedLogger.Value.Warning(
                $"preference:load:{steamId}",
                TimeSpan.FromMinutes(10),
                "[Localization] Не удалось загрузить язык игрока {SteamId}: {Error}",
                steamId,
                exception.Message);
        }
    }

    private void StatusCommand(ICommandContext context)
    {
        var snapshot = _cache.Value.Current;
        if (snapshot is null)
        {
            context.Reply("Localization.Core 1.4.0\nSnapshot: загружается");
            return;
        }

        context.Reply(
            $"Localization.Core 1.4.0\nSource: {snapshot.Source}\nKeys: {snapshot.Entries.Count}" +
            $"\nTags: {snapshot.Tags.Count}" +
            $"\nLanguages: {snapshot.Languages.Values.Count(language => language.Enabled)}" +
            $"\nServer fallback: {snapshot.Settings.ServerFallbackLanguage}" +
            $"\nVersion: {snapshot.Settings.ConfigurationVersion}");
    }

    private void ReloadCommand(ICommandContext context)
    {
        var playerId = context.Sender?.PlayerID;
        context.Reply("Localization reload started.");
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
                Core.PlayerManager.GetPlayer(id)?.SendChat($"[Localization] {result.Message}");
            }
            else
            {
                Core.Logger.LogInformation("[Localization] {Result}", result.Message);
            }
        });
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

        if (tasks.Length == 0)
        {
            return;
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
