using Common.Di;
using Common.Di.Utils;
using Microsoft.Extensions.Options;
using MoneySystem.Api;
using MoneySystem.Data.Configs;
using MoneySystem.Di;
using MoneySystem.Services;
using MSApi;
using MSCore.Generated;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZPApi;

namespace MoneySystem;

[PluginMetadata(
    Id = "MSCore", 
    Version = BuildInfo.Version,
    Name = "[ZP] MoneySystem",
    Author = "illusion & fdrinv",
    Description = "Manages money on the server"
)]
internal sealed partial class MoneySystem(ISwiftlyCore core) : Plugin<MoneySystemModule>(core)
{
    private Guid _guidOnPlayerHurtPost = Guid.Empty;
    
    private IZServiceApi _zServiceApi = null!;

    private readonly Lazy<IMoneyService> _moneyServiceLazy = GetRequiredServiceLazy<IMoneyService>();
    private readonly Lazy<IOptions<MoneySystemConfig>> _config = GetRequiredServiceLazy<IOptions<MoneySystemConfig>>();

    private const string ConVarMaxMoney = "mp_maxmoney";
    private const string ConVarStartMoney = "mp_startmoney";
    
    public override void UseSharedInterface(IInterfaceManager interfaceManager)
    {
        _zServiceApi = interfaceManager.GetSharedInterface<IZServiceApi>(IZServiceApi.SharedApiKey);
    }

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        var mSServiceApi = new MSServiceApi(_moneyServiceLazy.Value);
        interfaceManager.AddSharedInterface<IMSServiceApi, MSServiceApi>(IMSServiceApi.SharedApiKey, mSServiceApi);
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
        
        _zServiceApi.EventSubscriber.OnPlayerInfectedBy += OnPlayerInfectedBy;
    }

    protected override void OnUnload()
    {
        Core.GameEvent.Unhook(_guidOnPlayerHurtPost);

        _zServiceApi.EventSubscriber.OnPlayerInfectedBy -= OnPlayerInfectedBy;
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

        if (_zServiceApi.IsInfected(player) || victim.Controller.Team == player.Controller.Team)
        {
            return HookResult.Continue;
        }

        _moneyServiceLazy.Value.GiveMoney(player, @event.DmgHealth);

        return HookResult.Continue;
    }
}