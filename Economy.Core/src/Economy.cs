using Admin.Api;
using Common.Database.Migrator;
using Common.Di;
using CustomEquipment.Api;
using CustomEquipment.Api.Events.Contexts.Items;
using Economy.Api;
using Economy.Api.Events;
using Economy.Core.Api;
using Economy.Core.Database;
using Economy.Core.Di;
using Economy.Core.Services;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;
using ZombiePlague.Api.Events.Contexts;
using ZombiePlague.Api.Events.Contexts.Player;

namespace Economy.Core;

[PluginMetadata(
    Id = "Economy.Core",
    Version = "0.2.0",
    Name = "Economy",
    Author = "illusion & fdrinv",
    Description = "Manages economic on the server"
)]
internal sealed partial class Economy(ISwiftlyCore core) : Plugin<EconomyModule>(core)
{
    private Guid _guidOnPlayerHurtPost = Guid.Empty;
    private Guid _guidOnPlayerDeathPost = Guid.Empty;
    private Guid _guidOnPlayerConnectFullPost = Guid.Empty;
    private Guid _guidOnPlayerPlayerDisconnectPre = Guid.Empty;
    private Guid _roundPoststartHook = Guid.Empty;
    private Guid _roundEndHook = Guid.Empty;
    private bool _unloading;

    private IZombiePlagueApi _zombiePlagueApi = null!;

    private readonly Lazy<IEconomyService> _economyServiceLazy = GetRequiredServiceLazy<IEconomyService>();
    private readonly Lazy<IEconomyEvents> _economyEvents = GetRequiredServiceLazy<IEconomyEvents>();
    private readonly Lazy<PlayerAccountService> _playerAccountService = GetRequiredServiceLazy<PlayerAccountService>();
    private readonly Lazy<DatabaseMigrator<EconomyDbContext>> _databaseMigrator = GetRequiredServiceLazy<DatabaseMigrator<EconomyDbContext>>();
    private readonly Lazy<IEconomyRulesProvider> _rulesProvider = GetRequiredServiceLazy<IEconomyRulesProvider>();
    private readonly Lazy<EconomyRewardService> _rewardService = GetRequiredServiceLazy<EconomyRewardService>();
    private readonly Lazy<CustomWeaponHitTracker> _customWeaponHitTracker = GetRequiredServiceLazy<CustomWeaponHitTracker>();
    private readonly Lazy<EconomyExternalApis> _externalApis = GetRequiredServiceLazy<EconomyExternalApis>();
    private readonly Lazy<EconomyRuntimeCoordinator> _runtimeCoordinator = GetRequiredServiceLazy<EconomyRuntimeCoordinator>();

    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        _zombiePlagueApi = interfaceManager.GetSharedInterface<IZombiePlagueApi>(IZombiePlagueApi.SharedApiKey);

        TryBindOptionalApi(
            () => interfaceManager.GetSharedInterface<IAdminApi>(IAdminApi.SharedApiKey),
            api => _externalApis.Value.Admin = api,
            "Admin.Api"
        );
        TryBindOptionalApi(
            () => interfaceManager.GetSharedInterface<ICustomEquipmentApi>(ICustomEquipmentApi.SharedApiKey),
            api => _externalApis.Value.CustomEquipment = api,
            "CustomEquipment.Api"
        );
    }

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        var mSServiceApi = new EconomyApi(_economyServiceLazy.Value, _economyEvents.Value);
        interfaceManager.AddSharedInterface<IEconomyApi, EconomyApi>(IEconomyApi.SharedApiKey, mSServiceApi);
    }

    protected override void OnStart()
    {
        if (TryMigrateDatabase())
        {
            _rulesProvider.Value.InitializeFromDatabase();
        }
    }

    protected override void OnReady()
    {
        _unloading = false;
        _guidOnPlayerHurtPost = Core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurtPost);
        _guidOnPlayerDeathPost = Core.GameEvent.HookPost<EventPlayerDeath>(OnPlayerDeathPost);
        _guidOnPlayerConnectFullPost = Core.GameEvent.HookPost<EventPlayerConnectFull>(OnPlayerConnectFull);
        _guidOnPlayerPlayerDisconnectPre = Core.GameEvent.HookPre<EventPlayerDisconnect>(OnPlayerDisconnect);
        _roundPoststartHook = Core.GameEvent.HookPost<EventRoundPoststart>(OnRoundPostStart);
        _roundEndHook = Core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);

        _zombiePlagueApi.Events.Players.Infected.Hook(OnPlayerInfected);

        var customEquipmentApi = _externalApis.Value.CustomEquipment;

        if (customEquipmentApi is not null)
        {
            customEquipmentApi.Events.Weapons.DamageModified.Hook(OnCustomWeaponDamageModified);
        }

        Core.Scheduler.NextWorldUpdate(() =>
        {
            if (!_unloading)
            {
                InitializeConnectedPlayers();
            }
        });
        _runtimeCoordinator.Value.Start();
    }

    protected override void OnUnload()
    {
        _unloading = true;
        _runtimeCoordinator.Value.StopAndWait();

        Core.GameEvent.Unhook(_guidOnPlayerHurtPost);
        Core.GameEvent.Unhook(_guidOnPlayerDeathPost);
        Core.GameEvent.Unhook(_guidOnPlayerConnectFullPost);
        Core.GameEvent.Unhook(_guidOnPlayerPlayerDisconnectPre);
        Core.GameEvent.Unhook(_roundPoststartHook);
        Core.GameEvent.Unhook(_roundEndHook);

        _zombiePlagueApi.Events.Players.Infected.Unhook(OnPlayerInfected);

        var customEquipmentApi = _externalApis.Value.CustomEquipment;

        if (customEquipmentApi is not null)
        {
            customEquipmentApi.Events.Weapons.DamageModified.Unhook(OnCustomWeaponDamageModified);
        }

        _playerAccountService.Value.Shutdown(_rulesProvider.Value.Current.Persistence.SaveOnUnload);
    }

    private void OnPlayerInfected(ref PlayerInfectedContext context)
    {
        var infector = context.Infector;

        if (infector is not { IsValid: true, IsFakeClient: false })
        {
            return;
        }

        _rewardService.Value.RewardInfection(infector);
    }

    private HookResult OnPlayerHurtPost(EventPlayerHurt @event)
    {
        var player = @event.AttackerPlayer;
        var victim = @event.UserIdPlayer;

        if (player is not { IsValid: true, IsFakeClient: false }
            || victim is not { IsValid: true })
        {
            return HookResult.Continue;
        }

        if (_zombiePlagueApi.IsInfected(player) || !_zombiePlagueApi.IsInfected(victim))
        {
            return HookResult.Continue;
        }

        var customWeaponKey = _customWeaponHitTracker.Value.Consume(player, victim);
        var weaponKey = customWeaponKey ?? @event.Weapon;

        _rewardService.Value.RewardDamage(player, @event.ActualDmgHealth, weaponKey);

        return HookResult.Continue;
    }

    private HookResult OnPlayerDeathPost(EventPlayerDeath @event)
    {
        var attacker = @event.AttackerPlayer;
        var victim = @event.UserIdPlayer;

        if (attacker is not { IsValid: true, IsFakeClient: false }
            || victim is not { IsValid: true }
            || (victim.SteamID != 0 && attacker.SteamID == victim.SteamID))
        {
            return HookResult.Continue;
        }

        var attackerInfected = _zombiePlagueApi.IsInfected(attacker);
        var victimInfected = _zombiePlagueApi.IsInfected(victim);

        if (attackerInfected == victimInfected)
        {
            return HookResult.Continue;
        }

        if (victimInfected)
        {
            _rewardService.Value.RewardZombieKill(attacker);
        }
        else
        {
            _rewardService.Value.RewardHumanKill(attacker);
        }

        return HookResult.Continue;
    }

    private void OnCustomWeaponDamageModified(ref WeaponDamageModifiedContext context)
    {
        _customWeaponHitTracker.Value.Track(context);
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;

        if (player is not { IsValid: true, IsAuthorized: true, IsFakeClient: false })
        {
            return HookResult.Continue;
        }

        _playerAccountService.Value.Initialize(player);

        return HookResult.Continue;
    }

    private void InitializeConnectedPlayers()
    {
        foreach (var player in Core.PlayerManager.GetAllValidPlayers())
        {
            if (player is { IsAuthorized: true, IsFakeClient: false })
            {
                _playerAccountService.Value.Initialize(player);
            }
        }
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var player = @event.UserIdPlayer;

        if (player is null || player.IsFakeClient)
        {
            return HookResult.Continue;
        }

        _customWeaponHitTracker.Value.Remove(player.SteamID);
        _playerAccountService.Value.Remove(
            player,
            _rulesProvider.Value.Current.Persistence.SaveOnDisconnect
        );

        return HookResult.Continue;
    }

    private HookResult OnRoundPostStart(EventRoundPoststart @event)
    {
        Core.Scheduler.NextWorldUpdate(() =>
        {
            if (_unloading)
            {
                return;
            }

            foreach (var player in Core.PlayerManager.GetAllValidPlayers())
            {
                if (player.IsFakeClient || !player.IsAuthorized)
                {
                    continue;
                }

                _playerAccountService.Value.ReconcileLimit(player);
            }
        });

        return HookResult.Continue;
    }

    private HookResult OnRoundEnd(EventRoundEnd @event)
    {
        if (_rulesProvider.Value.Current.Persistence.SaveOnRoundEnd)
        {
            _playerAccountService.Value.SaveAll();
        }

        return HookResult.Continue;
    }

    private bool TryMigrateDatabase()
    {
        try
        {
            _databaseMigrator
                .Value
                .Migrate();

            return true;
        }
        catch (Exception exception)
        {
            Core.Logger.LogError(
                exception,
                "Economy database migration failed. Temporary balances will be used."
            );

            return false;
        }
    }

    private void TryBindOptionalApi<TApi>(
        Func<TApi> resolve,
        Action<TApi?> bind,
        string apiName)
        where TApi : class
    {
        try
        {
            bind(resolve());
        }
        catch (Exception exception)
        {
            bind(null);
            Core.Logger.LogWarning(
                exception,
                "Optional economy integration {ApiName} is unavailable.",
                apiName
            );
        }
    }
}
