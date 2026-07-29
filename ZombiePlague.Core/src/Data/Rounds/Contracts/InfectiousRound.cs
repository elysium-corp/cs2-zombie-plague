using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal abstract class InfectiousRound(
    ISwiftlyCore core,
    IZombieManager zombieManager,
    IHumanManager humanManager)
    : BaseRound(core)
{
    private Guid _onPlayerDeathEvent;
    private Guid _onPlayerConnectFullEvent;
    private bool _isActive;

    protected abstract bool ZombieRevived { get; }
    protected abstract float ZombieSpawnTime { get; }

    protected sealed override void OnStart()
    {
        _isActive = true;
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
        _isActive = false;
        Core.GameHooks.Entities.TakeDamage.Pre -= OnEntityTakeDamage;

        if (ZombieRevived)
        {
            Core.GameEvent.Unhook(_onPlayerDeathEvent);
            Core.GameEvent.Unhook(_onPlayerConnectFullEvent);
        }
    }

    protected abstract void OnInfectiousStart();
    

    private void OnEntityTakeDamage(ref TakeDamageEntityPreContext @event)
    {
        var attacker = @event.Params.Info.Attacker.ResolvePlayerFromHandle(Core);
        if (attacker == null || !attacker.IsValid || zombieManager.GetZombie(attacker) == null)
        {
            return;
        }

        var victim = @event.Params.Entity.Address.FindPlayerByPawnAddress(Core);
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
        attacker.IsValid
        && victim.IsValid
        && zombieManager.GetZombie(attacker) != null
        && zombieManager.GetZombie(victim) == null
        && !(humanManager.IsHuman(victim) && humanManager.GetHumanCount() == 1);

    protected virtual void Infect(IPlayer attacker, IPlayer victim) =>
        zombieManager.CreateZombie(victim, attacker);

    protected virtual HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        var player = @event.UserIdPlayer;
        Core.Scheduler.DelayBySeconds(ZombieSpawnTime, () =>
        {
            if (player == null || !player.IsValid || !_isActive)
            {
                return;
            }

            zombieManager.Respawn(player);
        });

        return HookResult.Continue;
    }

    protected virtual HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;
        Core.Scheduler.DelayBySeconds(ZombieSpawnTime, () =>
        {
            if (player == null || !player.IsValid || !_isActive)
            {
                return;
            }

            zombieManager.Respawn(player);
        });

        return HookResult.Continue;
    }
}
