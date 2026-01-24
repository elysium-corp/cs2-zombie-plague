using CS2ZombiePlague.Config;
using CS2ZombiePlague.Data.Events;
using CS2ZombiePlague.Data.Extensions;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Convars;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data;

public class MoneySystem(ISwiftlyCore core, IOptions<ZombiePlagueCoreConfig> config, IEventSubscriber eventSubscriber)
{
    public void Start()
    {
        core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurtPost);
        
        IConVar<int>? maxMoney = core.ConVar.Find<int>("mp_maxmoney");
        maxMoney!.Value = config.Value.MaxMoney;
        
        IConVar<int>? startMoney = core.ConVar.Find<int>("mp_startmoney");
        startMoney!.Value = config.Value.StartMoney;

        eventSubscriber.OnPlayerInfectedBy += OnPlayerInfectedBy;
    }
    
    private void OnPlayerInfectedBy(IPlayer infector, IPlayer player)
    {
        GiveMoney(infector, config.Value.MoneyForInfection);
    }
    
    private HookResult OnPlayerHurtPost(EventPlayerHurt @event)
    {
        var player = @event.AttackerPlayer;
        var victim = @event.UserIdPlayer;

        if (player == null || victim == null || !player.IsValid || !victim.IsValid)
            return HookResult.Continue;

        if (player.IsInfected() || victim.Controller.Team == player.Controller.Team)
        {
            return HookResult.Continue;
        }

        GiveMoney(player, @event.DmgHealth);

        return HookResult.Continue;
    }

    private void GiveMoney(IPlayer player, int count)
    {
        var playerMoneyService = player.Controller.InGameMoneyServices;

        var currentMoney = playerMoneyService.Account;
        var additionalMoney = count;

        playerMoneyService.Account = currentMoney + additionalMoney;
        playerMoneyService.AccountUpdated();
    }
}