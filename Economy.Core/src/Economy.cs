using Common.Database.Migrator;
using Common.Di;
using Common.Di.Utils;
using Economy.Api;
using Economy.Core.Api;
using Economy.Core.Data.Configs;
using Economy.Core.Database;
using Economy.Core.Di;
using Economy.Core.Initializer;
using Economy.Core.Services;
using Microsoft.Extensions.Logging;
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
    private Guid _guidOnPlayerHurtPost = Guid.Empty;
    private Guid _guidOnPlayerConnectFullPost = Guid.Empty;
    private Guid _guidOnPlayerPlayerDisconnectPre = Guid.Empty;
    private Guid _roundPoststartHook = Guid.Empty;

    private IZombiePlagueApi _zombiePlagueApi = null!;
    
    private readonly Lazy<IEconomyService> _economyServiceLazy = GetRequiredServiceLazy<IEconomyService>();
    private readonly Lazy<IOptions<EconomyConfig>> _config = GetRequiredServiceLazy<IOptions<EconomyConfig>>();
    private readonly Lazy<PlayerAccountService> _playerAccountService = GetRequiredServiceLazy<PlayerAccountService>();
    private readonly Lazy<DatabaseMigrator<EconomyDbContext>> _databaseMigrator = GetRequiredServiceLazy<DatabaseMigrator<EconomyDbContext>>();
    
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
        TryMigrateDatabase();
    }

    protected override void OnReady()
    {
        _guidOnPlayerHurtPost = Core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurtPost);
        _guidOnPlayerConnectFullPost = Core.GameEvent.HookPost<EventPlayerConnectFull>(OnPlayerConnectFull);
        _guidOnPlayerPlayerDisconnectPre = Core.GameEvent.HookPre<EventPlayerDisconnect>(OnPlayerDisconnect);
        _roundPoststartHook = Core.GameEvent.HookPost<EventRoundPoststart>(OnRoundPostStart);

        _zombiePlagueApi.EventSubscriber.OnPlayerInfected += OnPlayerInfected;
    }

    protected override void OnUnload()
    {
        Core.GameEvent.Unhook(_guidOnPlayerHurtPost);
        Core.GameEvent.Unhook(_guidOnPlayerConnectFullPost);
        Core.GameEvent.Unhook(_guidOnPlayerPlayerDisconnectPre);
        Core.GameEvent.Unhook(_roundPoststartHook);

        _zombiePlagueApi.EventSubscriber.OnPlayerInfected -= OnPlayerInfected;
        
        _playerAccountService.Value.SaveAllAndWait();
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

        if (player is not { IsValid: true, IsAuthorized: true, IsFakeClient: false })
        {
            return HookResult.Continue;
        }

        _playerAccountService.Value.Initialize(player);

        return HookResult.Continue;
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        var player = @event.UserIdPlayer;

        if (player is null || player.IsFakeClient)
        {
            return HookResult.Continue;
        }

        _playerAccountService.Value.Remove(player);

        return HookResult.Continue;
    }

    private HookResult OnRoundPostStart(EventRoundPoststart @event)
    {
        Core.Scheduler.NextWorldUpdate(() =>
        {
            foreach (var player in Core.PlayerManager.GetAllValidPlayers())
            {
                if (player.IsFakeClient || !player.IsAuthorized)
                {
                    continue;
                }

                _playerAccountService.Value.RefreshProjection(player);
            }
        });

        return HookResult.Continue;
    }
    
    private void TryMigrateDatabase()
    {
        try
        {
            _databaseMigrator
                .Value
                .Migrate();
        }
        catch (Exception exception)
        {
            Core.Logger.LogError(
                exception,
                "Economy database migration failed. Temporary balances will be used."
            );
        }
    }
}
