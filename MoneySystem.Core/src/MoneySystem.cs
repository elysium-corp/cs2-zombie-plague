using Common.Di;
using Common.Di.Utils;
using Microsoft.Extensions.Options;
using MoneySystem.Api;
using MoneySystem.Core.Api;
using MoneySystem.Core.Data.Configs;
using MoneySystem.Core.Di;
using MoneySystem.Core.Generated;
using MoneySystem.Core.Services;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;

namespace MoneySystem.Core;

[PluginMetadata(
    Id = "MoneySystem.Core", 
    Version = BuildInfo.Version,
    Name = "[ZP] MoneySystem",
    Author = "illusion & fdrinv",
    Description = "Manages money on the server"
)]
internal sealed partial class MoneySystem(ISwiftlyCore core) : Plugin<MoneySystemModule>(core)
{
    private Guid _guidOnPlayerHurtPost = Guid.Empty;
    
    private IZombiePlagueApi _zombiePlagueApi = null!;

    private readonly Lazy<IMoneyService> _moneyServiceLazy = GetRequiredServiceLazy<IMoneyService>();
    private readonly Lazy<IOptions<MoneySystemConfig>> _config = GetRequiredServiceLazy<IOptions<MoneySystemConfig>>();

    private const string ConVarMaxMoney = "mp_maxmoney";
    private const string ConVarStartMoney = "mp_startmoney";
    
    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
        _zombiePlagueApi = interfaceManager.GetSharedInterface<IZombiePlagueApi>(IZombiePlagueApi.SharedApiKey);
    }

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        var mSServiceApi = new MoneySystemApi(_moneyServiceLazy.Value);
        interfaceManager.AddSharedInterface<IMoneySystemApi, MoneySystemApi>(IMoneySystemApi.SharedApiKey, mSServiceApi);
    }

    protected override void OnStart()
    {
        var conVarService = Core.ConVar;
        var maxMoneyAsCVar = conVarService.Find<int>(ConVarMaxMoney);
        var startMoneyAsCVar = conVarService.Find<int>(ConVarStartMoney);
        var config = _config.Get();

        maxMoneyAsCVar?.Value = config.MaxMoney;
        startMoneyAsCVar?.Value = config.StartMoney;
    }

    protected override void OnReady()
    {
        _guidOnPlayerHurtPost = Core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurtPost);
        
        _zombiePlagueApi.EventSubscriber.OnPlayerInfectedBy += OnPlayerInfectedBy;
    }

    protected override void OnUnload()
    {
        Core.GameEvent.Unhook(_guidOnPlayerHurtPost);

        _zombiePlagueApi.EventSubscriber.OnPlayerInfectedBy -= OnPlayerInfectedBy;
    }
    
    
    
    private void OnPlayerInfectedBy(IPlayer infector, IPlayer player)
    {
        var config = _config.Value.Value;
        _moneyServiceLazy.Value.GiveMoney(infector, config.MoneyForInfection);
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

        _moneyServiceLazy.Value.GiveMoney(player, @event.DmgHealth);

        return HookResult.Continue;
    }
}