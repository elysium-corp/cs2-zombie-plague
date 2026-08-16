using CustomKnife.Data.Menus;
using CustomKnife.Data.Services.Contracts;
using CustomKnife.Initializer;
using Menu.Api.Extensions;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using ZombiePlague.Api.Menus;

namespace CustomKnife;

internal sealed class CustomKnifeCoordinator(
    ISwiftlyCore core,
    IKnifeService knifeService,
    IPlayerKnifeService playerKnifeService,
    KnifeRegistryInitializer knifeRegistryInitializer,
    KnifeMenu knifeMenu,
    MenuApiBridge menuApiBridge
)
{
    private Guid _playerEquipHook = Guid.Empty;
    private Guid _playerSpawnHook = Guid.Empty;
    private Guid _playerHurtHook = Guid.Empty;
    private Guid _roundStartHook = Guid.Empty;
    private Guid _playerDisconnectHook = Guid.Empty;

    private IDisposable? _mainMenuSubscription;

    private const string SelectKnifeItemTitle = "Menu.Main.Item.Knife.Title";

    public void Start()
    {
        knifeRegistryInitializer.Initialize();

        knifeMenu.RegisterCommands();

        RegisterEvents();
        RegisterMenuExtensions();
    }

    public void Stop()
    {
        knifeMenu.UnregisterCommands();

        UnregisterEvents();
        UnregisterMenuExtensions();
    }

    private void RegisterEvents()
    {
        core.Event.OnClientSteamAuthorize += OnClientSteamAuthorize;
        
        _playerEquipHook = core.GameEvent.HookPost<EventItemEquip>(OnPlayerEquip);
        _playerSpawnHook = core.GameEvent.HookPost<EventPlayerSpawn>(OnPlayerSpawn);
        _playerHurtHook = core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurt);
        _roundStartHook = core.GameEvent.HookPost<EventRoundStart>(OnRoundStart);
        _playerDisconnectHook = core.GameEvent.HookPost<EventPlayerDisconnect>(OnPlayerDisconnect);

        core.GameHooks.Entities.TakeDamage.Pre += OnEntityTakeDamage;
    }

    private void RegisterMenuExtensions()
    {
        _mainMenuSubscription = menuApiBridge.Extensions.Subscribe(
            menuId: ZombiePlagueMenuIds.Main,
            handler: ExtendMainMenu
        );
    }

    private void UnregisterEvents()
    {
        core.Event.OnClientSteamAuthorize -= OnClientSteamAuthorize;
        
        core.GameEvent.Unhook(_playerEquipHook);
        core.GameEvent.Unhook(_playerSpawnHook);
        core.GameEvent.Unhook(_playerHurtHook);
        core.GameEvent.Unhook(_roundStartHook);
        core.GameEvent.Unhook(_playerDisconnectHook);

        core.GameHooks.Entities.TakeDamage.Pre -= OnEntityTakeDamage;
    }

    private void UnregisterMenuExtensions()
    {
        _mainMenuSubscription?.Dispose();
        _mainMenuSubscription = null;
    }

    private void ExtendMainMenu(MenuExtensionContext context)
    {
        var localizer = core.Translation.GetPlayerLocalizer(context.Player);

        var knifeButton = new ButtonMenuOption(localizer[SelectKnifeItemTitle]);

        knifeButton.Click += (_, args) =>
        {
            core.Scheduler.NextTickAsync(() => knifeMenu.Open(args.Player));

            return ValueTask.CompletedTask;
        };

        context.Options.Add(knifeButton, 2);
    }

    private void OnEntityTakeDamage(ref TakeDamageEntityPreContext context)
    {
        knifeService.TryApplyKnifeDamage(ref context);
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn @event)
    {
        var player = @event.UserIdPlayer;

        if (player is null)
        {
            return HookResult.Continue;
        }

        knifeService.TryGiveKnife(player);

        return HookResult.Continue;
    }

    private HookResult OnRoundStart(EventRoundStart @event)
    {
        foreach (var player in core.PlayerManager.GetAlive())
        {
            core.Scheduler.NextWorldUpdate(() => knifeService.TryGiveKnife(player));
        }

        return HookResult.Continue;
    }

    private HookResult OnPlayerHurt(EventPlayerHurt @event)
    {
        var player = @event.UserIdPlayer;

        core.Scheduler.NextTick(() => knifeService.TryApplyProperties(player));

        knifeService.TryApplyKnifeKnockback(@event);

        return HookResult.Continue;
    }

    private HookResult OnPlayerEquip(EventItemEquip @event)
    {
        knifeService.TryApplyProperties(@event.UserIdPlayer);

        return HookResult.Continue;
    }
    
    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var steamId = @event.XuID;
        
        if (steamId == 0)
        {
            return HookResult.Continue;
        }

        playerKnifeService.Remove(steamId);

        return HookResult.Continue;
    }
    
    private void OnClientSteamAuthorize(IOnClientSteamAuthorizeEvent @event)
    {
        var player = core.PlayerManager.GetPlayer(@event.PlayerId);

        if (player is null || player.IsFakeClient || player.SteamID == 0)
        {
            return;
        }

        playerKnifeService.Initialize(player.SteamID);
    }
}