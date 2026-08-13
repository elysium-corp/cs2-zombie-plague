using Common.Di;
using Common.Di.Utils;
using Economy.Api;
using Economy.Core.Api;
using Economy.Core.Data.Configs;
using Economy.Core.Di;
using Economy.Core.Initializer;
using Economy.Core.Services;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;

namespace Economy.Core;

[PluginMetadata(
    Id = "Economy.Core",
    Version = "0.1.0",
    Name = "Economy",
    Author = "illusion & fdrinv",
    Description = "Manages economic on the server"
)]
internal sealed partial class Economy(ISwiftlyCore core) : Plugin<EconomyModule>(core)
{
    private readonly Dictionary<ulong, int> _balancesBeforeRestart = [];

    private Guid _guidOnPlayerHurtPost = Guid.Empty;
    private Guid _guidOnPlayerConnectFullPost = Guid.Empty;
    private Guid _guidOnPlayerPlayerDisconnectPre = Guid.Empty;
    private Guid _csPreRestartHook = Guid.Empty;
    private Guid _roundPoststartHook = Guid.Empty;

    private IZombiePlagueApi _zombiePlagueApi = null!;

    private readonly Lazy<EconomyDatabaseInitializer> _economyDatabaseInitializer = GetRequiredServiceLazy<EconomyDatabaseInitializer>();
    
    private readonly Lazy<IEconomyService> _economyServiceLazy = GetRequiredServiceLazy<IEconomyService>();
    private readonly Lazy<IOptions<EconomyConfig>> _config = GetRequiredServiceLazy<IOptions<EconomyConfig>>();
    private readonly Lazy<IAccountPersistenceService> _accountPersistenceService = GetRequiredServiceLazy<IAccountPersistenceService>();
    
    
    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        _zombiePlagueApi = interfaceManager.GetSharedInterface<IZombiePlagueApi>(IZombiePlagueApi.SharedApiKey);
    }

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        var mSServiceApi = new EconomyApi(_economyServiceLazy.Value);
        interfaceManager.AddSharedInterface<IEconomyApi, EconomyApi>(IEconomyApi.SharedApiKey, mSServiceApi);
    }

    protected override void OnStart()
    {
        _economyDatabaseInitializer.Value.Initialize();
    }

    protected override void OnReady()
    {
        _guidOnPlayerHurtPost = Core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurtPost);
        _guidOnPlayerConnectFullPost = Core.GameEvent.HookPost<EventPlayerConnectFull>(OnPlayerConnectFull);
        _guidOnPlayerPlayerDisconnectPre = Core.GameEvent.HookPre<EventPlayerDisconnect>(OnPlayerDisconnect);
        _csPreRestartHook = Core.GameEvent.HookPre<EventCsPreRestart>(OnCsPreRestart);
        _roundPoststartHook = Core.GameEvent.HookPost<EventRoundPoststart>(OnRoundPoststart);

        _zombiePlagueApi.EventSubscriber.OnPlayerInfected += OnPlayerInfected;
    }

    protected override void OnUnload()
    {
        Core.GameEvent.Unhook(_guidOnPlayerHurtPost);
        Core.GameEvent.Unhook(_guidOnPlayerConnectFullPost);
        Core.GameEvent.Unhook(_guidOnPlayerPlayerDisconnectPre);
        Core.GameEvent.Unhook(_csPreRestartHook);
        Core.GameEvent.Unhook(_roundPoststartHook);

        _zombiePlagueApi.EventSubscriber.OnPlayerInfected -= OnPlayerInfected;
    }

    private void OnPlayerInfected(IPlayer _, IPlayer? infector)
    {
        if (infector is not { IsValid: true })
        {
            return;
        }

        var config = _config.Value.Value;
        _economyServiceLazy.Value.GiveMoney(infector, config.MoneyForInfection);
    }

    private HookResult OnPlayerHurtPost(EventPlayerHurt @event)
    {
        var player = @event.AttackerPlayer;
        var victim = @event.UserIdPlayer;

        if (player == null || victim == null || !player.IsValid || !victim.IsValid) return HookResult.Continue;

        if (_zombiePlagueApi.IsInfected(player) || victim.Controller.Team == player.Controller.Team)
        {
            return HookResult.Continue;
        }

        var money = (int)Math.Floor(@event.ActualDmgHealth * _config.Value.Value.MoneyForDamage);

        _economyServiceLazy.Value.GiveMoney(player, money);

        return HookResult.Continue;
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;

        if (player == null || !player.IsValid || !player.IsAuthorized || player.IsFakeClient)
        {
            return HookResult.Continue;
        }

        var steamId = (long)player.SteamID;
        var initialBalance = _config.Get().StartMoney;

        var balance = _accountPersistenceService.Value.LoadOrCreateBalance(steamId, initialBalance);

        _economyServiceLazy.Value.SetBalance(player, balance);

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var player = @event.UserIdPlayer;

        if (player == null || !player.IsValid || player.IsFakeClient)
        {
            return HookResult.Continue;
        }

        var steamId = (long)player.SteamID;
        var balance = _economyServiceLazy.Value.GetBalance(player);

        _accountPersistenceService.Value.SaveBalance(steamId, balance);

        return HookResult.Continue;
    }

    private HookResult OnCsPreRestart(EventCsPreRestart @event)
    {
        _balancesBeforeRestart.Clear();

        foreach (var player in Core.PlayerManager.GetAllValidPlayers())
        {
            if (player.IsFakeClient || !player.IsAuthorized)
            {
                continue;
            }

            _balancesBeforeRestart[player.SteamID] = _economyServiceLazy.Value.GetBalance(player);
        }

        return HookResult.Continue;
    }

    private HookResult OnRoundPoststart(EventRoundPoststart @event)
    {
        if (_balancesBeforeRestart.Count == 0)
        {
            return HookResult.Continue;
        }

        var balances = _balancesBeforeRestart.ToArray();
        _balancesBeforeRestart.Clear();

        Core.Scheduler.NextWorldUpdate(() =>
        {
            foreach (var (steamId, balance) in balances)
            {
                var player = Core.PlayerManager.GetPlayerFromSteamId(steamId, allowUnauthorized: false);

                if (player == null || !player.IsValid)
                {
                    continue;
                }

                _economyServiceLazy.Value.SetBalance(player, balance);
            }
        });

        return HookResult.Continue;
    }
}
