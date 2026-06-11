using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal abstract class InfectiousRound(ISwiftlyCore core, RoundManager roundManager, ZombieManager zombieManager)
    : BaseRound(core, roundManager, zombieManager)
{
    private Guid _onPlayerDeathEvent;
    private Guid _onPlayerConnectFullEvent;

    protected abstract bool ZombieRevived { get; }
    protected abstract float ZombieSpawnTime { get; }

    protected sealed override void OnStart()
    {
        Core.GameHooks.Entities.TakeDamage.Pre += OnEntityTakeDamage;

        if (ZombieRevived)
        {
            _onPlayerDeathEvent = Core.GameEvent.HookPre<EventPlayerDeath>(OnPlayerDeath);
            _onPlayerConnectFullEvent = Core.GameEvent.HookPre<EventPlayerConnectFull>(OnPlayerConnectFull);
        }

        OnInfectiousStart();
    }

    protected sealed override void OnEnd()
    {
        Core.GameHooks.Entities.TakeDamage.Pre -= OnEntityTakeDamage;

        if (ZombieRevived)
        {
            Core.GameEvent.Unhook(_onPlayerDeathEvent);
            Core.GameEvent.Unhook(_onPlayerConnectFullEvent);
        }

        OnInfectiousEnd();
    }

    protected abstract void OnInfectiousStart();

    protected virtual void OnInfectiousEnd()
    {
    }

    private void OnEntityTakeDamage(ref TakeDamageEntityPreContext @event)
    {
        var attacker = @event.Params.Info.Attacker.ResolvePlayerFromHandle();
        if (attacker == null || !attacker.IsValid || !attacker.IsInfected())
        {
            return;
        }

        var victim = @event.Params.Entity.Address.FindPlayerByPawnAddress();
        if (victim == null || !victim.IsValid || victim.PlayerPawn is not { } pawn)
        {
            return;
        }

        if (!CanInfect(attacker, victim))
            return;

        if (pawn.ArmorValue > 0)
        {
            victim.SetArmor(Math.Max((int)(pawn.ArmorValue - @event.Params.Info.Damage), 0));
            @event.Params.Info.Damage = 0;
            return;
        }

        Infect(attacker, victim);
        @event.Params.Info.Damage = 0;
    }

    protected virtual bool CanInfect(IPlayer attacker, IPlayer victim) =>
        attacker.IsValid && victim.IsValid && attacker.IsInfected()
        && !victim.IsInfected() && !victim.IsLastHuman();

    protected virtual void Infect(IPlayer attacker, IPlayer victim) =>
        ZombieManager.CreateZombie(victim, attacker);

    protected virtual HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        var player = @event.UserIdPlayer;
        Core.Scheduler.DelayBySeconds(ZombieSpawnTime, () =>
        {
            if (player == null || !player.IsValid || RoundManager.GetRound() != this)
            {
                return;
            }

            ZombieManager.Respawn(player);
        });

        return HookResult.Continue;
    }

    protected virtual HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;
        Core.Scheduler.DelayBySeconds(ZombieSpawnTime, () =>
        {
            if (player == null || !player.IsValid || RoundManager.GetRound() != this)
            {
                return;
            }

            ZombieManager.Respawn(player);
        });

        return HookResult.Continue;
    }
}