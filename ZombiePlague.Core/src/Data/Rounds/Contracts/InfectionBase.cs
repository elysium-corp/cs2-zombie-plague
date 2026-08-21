using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal abstract class InfectionBase(
    ISwiftlyCore core, 
    IPlayerManager playerManager
) : RoundBase(core, playerManager)
{
    protected override void OnTakeDamage(ref TakeDamageEntityPreContext context)
    {
        var attacker = context.Params.Info.Attacker.ResolvePlayerFromHandle();

        if (attacker is not { IsValid: true } || !attacker.IsAlive) return;

        var victim = context.Params.Entity.Address.FindPlayerByPawnAddress();

        if (victim is not { IsValid: true } || !victim.IsAlive || victim.PlayerPawn is not { } pawn) return;

        if (!CanInfect(attacker, victim)) return;

        var damage = (int)Math.Ceiling(context.Params.Info.Damage);
        var armor = pawn.ArmorValue;

        context.Params.Info.Damage = 0;

        if (armor > 0)
        {
            var remainingArmor = Math.Max(armor - damage, 0);
            victim.SetArmor(remainingArmor);

            return;
        }

        PlayerManager.TryInfect(victim, attacker);
    }
    
    protected override HookResult OnPlayerConnectedFull(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;

        if (player is not { IsValid: true })
        {
            return HookResult.Continue;
        }

        if (!PlayerManager.IsZombie(player) && !PlayerManager.TryInfect(player))
        {
            return HookResult.Continue;
        }

        Core.Scheduler.NextWorldUpdate(() => RespawnConnectedZombie(player));

        return HookResult.Continue;
    }

    private void RespawnConnectedZombie(IPlayer player)
    {
        if (!player.IsValid || player.IsAlive || !PlayerManager.IsZombie(player))
        {
            return;
        }

        PlayerManager.TryRespawn(player);
    }

    private bool CanInfect(IPlayer attacker, IPlayer victim)
    {
        if (!PlayerManager.IsZombie(attacker) || !PlayerManager.IsHuman(victim)) return false;

        var aliveHumanCount = PlayerManager
            .GetAllHumans()
            .Count(player => player.IsAlive);

        return aliveHumanCount > 1;
    }
}