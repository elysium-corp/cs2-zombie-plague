using Advertisement.Core.Application;
using Advertisement.Core.Data;
using Advertisement.Core.Database;
using Advertisement.Core.Di;
using Common.Database.Migrator;
using Common.Di;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;

namespace Advertisement.Core;

[PluginMetadata(
    Id = "Advertisement.Core",
    Version = "1.1.1",
    Name = "Elysium Advertisements",
    Author = "Elysium",
    Description = "Локализованная реклама и информационные сообщения с управлением через Flute CMS.")]
internal sealed class AdvertisementPlugin(ISwiftlyCore core) : Plugin<AdvertisementModule>(core)
{
    private readonly List<Guid> _commands = [];
    private readonly Lazy<AdvertisementCache> _cache = GetRequiredServiceLazy<AdvertisementCache>();
    private readonly Lazy<AdvertisementCoordinator> _coordinator = GetRequiredServiceLazy<AdvertisementCoordinator>();
    private readonly Lazy<AdvertisementScheduler> _scheduler = GetRequiredServiceLazy<AdvertisementScheduler>();
    private readonly Lazy<PlayerPreferenceRepository> _preferences = GetRequiredServiceLazy<PlayerPreferenceRepository>();
    private readonly Lazy<PlayerLocaleStore> _localeStore = GetRequiredServiceLazy<PlayerLocaleStore>();
    private readonly Lazy<PlayerLocaleResolver> _localeResolver = GetRequiredServiceLazy<PlayerLocaleResolver>();
    private readonly Lazy<RateLimitedLogger> _rateLimitedLogger = GetRequiredServiceLazy<RateLimitedLogger>();
    private readonly Lazy<DatabaseMigrator<AdvertisementDbContext>> _databaseMigrator =
        GetRequiredServiceLazy<DatabaseMigrator<AdvertisementDbContext>>();

    private CancellationTokenSource? _schedulerTimer;
    private Guid? _chatHook;

    protected override void OnStart()
    {
        TryMigrateDatabase();

        Core.Event.OnMapLoad += OnMapLoad;
        Core.Event.OnClientSteamAuthorize += OnClientSteamAuthorize;
        Core.Event.OnClientDisconnected += OnClientDisconnected;
        RegisterCommands();

        // Загрузка конфигурации не зависит от игрового состояния и может начинаться сразу.
        _coordinator.Value.Start();
    }

    protected override void OnReady()
    {
        // GlobalVars и текущее имя карты гарантированно доступны только после
        // завершения инициализации SwiftlyS2 и внедрения shared interfaces.
        _scheduler.Value.StartFromCurrentMap();
        _schedulerTimer = Core.Scheduler.RepeatBySeconds(1f, _scheduler.Value.Tick);

        foreach (var player in Core.PlayerManager.GetAllPlayers().Where(player => player.IsAuthorized))
        {
            BindAndLoadLocale(player.PlayerID, player.SteamID);
        }

        Core.Logger.LogInformation("[Advertisement] Advertisement.Core 1.1.1 загружен.");
    }

    protected override void OnUnload()
    {
        if (_chatHook is { } hook)
        {
            Core.Command.UnhookClientChat(hook);
            _chatHook = null;
        }

        foreach (var command in _commands)
        {
            Core.Command.UnregisterCommand(command);
        }

        _commands.Clear();

        _schedulerTimer?.Cancel();
        _schedulerTimer = null;

        Core.Event.OnMapLoad -= OnMapLoad;
        Core.Event.OnClientSteamAuthorize -= OnClientSteamAuthorize;
        Core.Event.OnClientDisconnected -= OnClientDisconnected;

        _coordinator.Value.Dispose();
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
                "[Advertisement] Миграция PostgreSQL не выполнена. Плагин продолжит работу с fallback-конфигурацией."
            );
        }
    }

    private void RegisterCommands()
    {
        _commands.Add(Core.Command.RegisterCommand("ads_status", StatusCommand, registerRaw: true,
            permission: "advertisement.admin", helpText: "Показать состояние Advertisement.Core"));
        _commands.Add(Core.Command.RegisterCommand("ads_reload", ReloadCommand, registerRaw: true,
            permission: "advertisement.admin", helpText: "Перезагрузить рекламу из PostgreSQL"));
        _commands.Add(Core.Command.RegisterCommand("ads_test", TestCommand, registerRaw: true,
            permission: "advertisement.admin", helpText: "Показать сообщение: ads_test <key> [locale]"));
        _commands.Add(Core.Command.RegisterCommand("lang", LanguageCommand, registerRaw: false,
            helpText: "Выбрать язык сообщений"));
        _chatHook = Core.Command.HookClientChat(OnClientChat);
    }

    private void OnMapLoad(IOnMapLoadEvent @event) => _scheduler.Value.OnMapLoaded(@event.MapName);

    private void OnClientSteamAuthorize(IOnClientSteamAuthorizeEvent @event)
    {
        var player = Core.PlayerManager.GetPlayer(@event.PlayerId);
        if (player is not null && player.IsAuthorized) BindAndLoadLocale(@event.PlayerId, player.SteamID);
    }

    private void OnClientDisconnected(IOnClientDisconnectedEvent @event) => _localeStore.Value.RemoveSlot(@event.PlayerId);

    private HookResult OnClientChat(int playerId, string text, bool teamOnly)
    {
        var value = text.Trim();
        if (!value.Equals("!lang", StringComparison.OrdinalIgnoreCase)
            && !value.Equals("!language", StringComparison.OrdinalIgnoreCase)
            && !value.Equals("!язык", StringComparison.OrdinalIgnoreCase)) return HookResult.Continue;
        var player = Core.PlayerManager.GetPlayer(playerId);
        if (player is not null) OpenLanguageMenu(player);
        return HookResult.Handled;
    }

    private void LanguageCommand(ICommandContext context)
    {
        if (context.Sender is null) { context.Reply("Команда доступна только игроку."); return; }
        OpenLanguageMenu(context.Sender);
    }

    private void OpenLanguageMenu(IPlayer player)
    {
        var settings = _cache.Value.Current?.Settings;
        if (settings is null) { player.SendChat("[Advertisement] Система ещё загружается."); return; }
        var locale = _localeResolver.Value.Resolve(player, settings);
        var builder = Core.MenusAPI.CreateBuilder().EnableExit().SetPlayerFrozen(false).Design.SetMenuTitle("Язык сообщений");
        builder.AddOption(CreateLanguageButton(player, null, "Автоматически (язык CS2)"));
        foreach (var item in settings.AllowedLocales.OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
            builder.AddOption(CreateLanguageButton(player, item, LanguageLabel(item)));
        Core.MenusAPI.OpenMenuForPlayer(player, builder.Build());
    }

    private ButtonMenuOption CreateLanguageButton(IPlayer player, string? locale, string title)
    {
        var button = new ButtonMenuOption(title) { CloseAfterClick = true };
        var playerId = player.PlayerID;
        var steamId = player.SteamID;
        button.Click += async (_, _) => await SaveLocaleAsync(playerId, steamId, locale);
        return button;
    }

    private async Task SaveLocaleAsync(int playerId, ulong steamId, string? locale)
    {
        try
        {
            await _preferences.Value.SaveLocaleAsync(steamId, locale, CancellationToken.None);
            _localeStore.Value.Set(steamId, locale);
            Core.Scheduler.NextTick(() => Core.PlayerManager.GetPlayer(playerId)?.SendChat(
                locale is null ? "[Advertisement] Язык: автоматически." : $"[Advertisement] Язык: {LanguageLabel(locale)}."));
        }
        catch (Exception exception)
        {
            _rateLimitedLogger.Value.Warning($"locale:save:{steamId}", TimeSpan.FromMinutes(2),
                "[Advertisement] Не удалось сохранить locale игрока {SteamId}: {Error}", steamId, exception.Message);
        }
    }

    private void BindAndLoadLocale(int playerId, ulong steamId)
    {
        _localeStore.Value.BindSlot(playerId, steamId);
        _ = LoadLocaleAsync(steamId);
    }

    private async Task LoadLocaleAsync(ulong steamId)
    {
        try { _localeStore.Value.Set(steamId, await _preferences.Value.LoadLocaleAsync(steamId, CancellationToken.None)); }
        catch (Exception exception)
        {
            _rateLimitedLogger.Value.Warning($"locale:load:{steamId}", TimeSpan.FromMinutes(10),
                "[Advertisement] Не удалось загрузить locale игрока {SteamId}: {Error}", steamId, exception.Message);
        }
    }

    private void StatusCommand(ICommandContext context)
    {
        var snapshot = _cache.Value.Current;
        if (snapshot is null) { context.Reply("Advertisement.Core 1.1.1\nSnapshot: загружается"); return; }
        var players = Core.PlayerManager.GetAllPlayers().ToArray();
        var bots = players.Count(x => x.IsFakeClient);
        var count = snapshot.Settings.ExcludeBotsFromPlayers ? players.Length - bots : players.Length;
        context.Reply($"Advertisement.Core 1.1.1\nSource: {snapshot.Source}\nMessages: {snapshot.Messages.Count}\nActive: {snapshot.ActiveMessageCount(DateTimeOffset.UtcNow, count)}\nDefault locale: {snapshot.Settings.DefaultLocale}\nVersion: {snapshot.Settings.ConfigurationVersion}");
    }

    private void ReloadCommand(ICommandContext context)
    {
        var playerId = context.Sender?.PlayerID;
        context.Reply("Advertisement reload started.");
        _ = ReloadAsync(playerId);
    }

    private async Task ReloadAsync(int? playerId)
    {
        var result = await _coordinator.Value.ReloadNowAsync();
        Core.Scheduler.NextTick(() =>
        {
            if (playerId is { } id) Core.PlayerManager.GetPlayer(id)?.SendChat($"[Advertisement] {result.Message}");
            else Core.Logger.LogInformation("[Advertisement] {Result}", result.Message);
        });
    }

    private void TestCommand(ICommandContext context)
    {
        if (context.Sender is null || context.Args.Length == 0) { context.Reply("Использование: ads_test <key> [locale]"); return; }
        var message = _cache.Value.Current?.Messages.Values.FirstOrDefault(x => x.Key.Equals(context.Args[0], StringComparison.OrdinalIgnoreCase));
        if (message is null) { context.Reply($"Сообщение '{context.Args[0]}' не найдено."); return; }
        _scheduler.Value.SendTest(message, context.Sender, context.Args.Length > 1 ? LocaleNormalizer.Normalize(context.Args[1]) : null);
    }

    private static string LanguageLabel(string locale) => LocaleNormalizer.Normalize(locale) switch
    {
        "ru" => "Русский", "en" => "English", "uk" => "Українська", "pl" => "Polski", "de" => "Deutsch",
        "pt-BR" => "Português (Brasil)", "zh-CN" => "简体中文", "zh-TW" => "繁體中文", var value => value.ToUpperInvariant(),
    };
}
