using Common.Hooks.Abstractions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal abstract class InfectionBase(
    ISwiftlyCore core, 
    IPlayerManager playerManager,
    IHookPublisher hooks
) : RoundBase(core, playerManager, hooks)
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

    private bool CanInfect(IPlayer attacker, IPlayer victim)
    {
        if (!PlayerManager.IsZombie(attacker) || !PlayerManager.IsHuman(victim)) return false;

        var aliveHumanCount = PlayerManager
            .GetAllHumans()
            .Count(player => player.IsAlive);

        return aliveHumanCount > 1;
    }
}