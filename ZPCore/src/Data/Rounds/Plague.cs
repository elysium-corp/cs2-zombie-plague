using System;
using System.Linq;
using ZPCore.Config;
using ZPCore.Config.Round;
using ZPCore.Data.Extensions;
using ZPCore.Data.Managers;
using ZPCore.Data.Rounds.Contracts;
using ZPCore.Utils.Extensions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Rounds;

internal class Plague(
    ISwiftlyCore core,
    RoundManager roundManager,
    ZombieManager zombieManager,
    PlagueConfig config) : BaseRound
{
    private Guid _onPlayerDeathEvent;
    private Guid _onPlayerConnectFullEvent;

    public override int Chance => config.Chance;
    public override string Name => "Чума";

    public override void Start()
    {
        core.Event.OnEntityTakeDamage += OnEntityTakeDamage;

        if (config.ZombieRevived)
        {
            _onPlayerDeathEvent = core.GameEvent.HookPre<EventPlayerDeath>(OnPlayerDeath);
            _onPlayerConnectFullEvent = core.GameEvent.HookPre<EventPlayerConnectFull>(OnPlayerConnectFull);
        }

        var players = core.PlayerManager.GetAlive().ToList();
        var countZombies = Math.Ceiling(players.Count * config.ZombieSpawnRatio);
        var newPlayers = players.Shuffle();

        foreach (var player in newPlayers)
        {
            if (player.IsValid)
            {
                zombieManager.CreateZombie(player);
                countZombies--;
            }

            if (countZombies == 0)
            {
                break;
            }
        }

        core.PlayerManager.SendCenter("Массовое заражение!");
    }

    public override void End()
    {
        core.Event.OnEntityTakeDamage -= OnEntityTakeDamage;

        if (config.ZombieRevived)
        {
            core.GameEvent.Unhook(_onPlayerDeathEvent);
            core.GameEvent.Unhook(_onPlayerConnectFullEvent);
        }

        roundManager.SetRound(new None());

        core.PlayerManager.SendCenter("Раунд окончен");
    }

    private void OnEntityTakeDamage(IOnEntityTakeDamageEvent @event)
    {
        var attacker = @event.Info.Attacker.ResolvePlayerFromHandle();
        var victim = @event.Entity.Address.FindPlayerByPawnAddress();

        if (attacker == null || victim == null || victim.PlayerPawn == null)
        {
            return;
        }

        if (!CanInfect(attacker, victim))
        {
            return;
        }

        if (victim.PlayerPawn.ArmorValue > 0)
        {
            var victimArmor = victim.PlayerPawn.ArmorValue;
            var finalArmor = (int)(victimArmor - @event.Info.Damage);
            var armor = finalArmor > 0 ? finalArmor : 0;

            victim.SetArmor(armor);
            @event.Info.Damage = 0;

            return;
        }

        zombieManager.CreateZombie(victim, attacker);

        @event.Info.Damage = 0;
    }

    protected override bool CanInfect(IPlayer attacker, IPlayer victim)
    {
        if (!victim.IsValid || !attacker.IsValid)
        {
            return false;
        }

        if (!attacker.IsInfected())
        {
            return false;
        }

        if (victim.IsInfected() || victim.IsLastHuman())
        {
            return false;
        }

        return true;
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        var player = @event.UserIdPlayer;
        core.Scheduler.DelayBySeconds(config.ZombieSpawnTime, () =>
        {
            if (player == null || !player.IsValid || roundManager.GetRound() != this)
            {
                return;
            }

            zombieManager.Respawn(player);
        });

        return HookResult.Continue;
    }

    private HookResult OnPlayerConnectFull(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;
        core.Scheduler.DelayBySeconds(config.ZombieSpawnTime, () =>
        {
            if (player == null || !player.IsValid || roundManager.GetRound() != this)
            {
                return;
            }

            zombieManager.Respawn(player);
        });

        return HookResult.Continue;
    }
}