using Localization.Api;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Core.Config.Core;
using ZombiePlague.Core.Config.Round;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Rounds.Respawn;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal abstract class InfectionBase(
    ISwiftlyCore core, 
    IPlayerManager playerManager,
    IOptions<ZombiePlagueCoreConfig> coreConfig,
    Func<ILocalizationApi> localization,
    IZombieRespawnConfig respawnConfig
) : RoundBase(core, playerManager, localization)
{
    private readonly ZombieRespawnTracker _respawnTracker = new(respawnConfig);
    private readonly Dictionary<ulong, CancellationTokenSource> _respawnTimers = [];

    private const int MaxRespawnRetryAttempts = 20;
    private const float RespawnRetryDelaySeconds = 0.1f;

    protected override void OnTakeDamage(ref TakeDamageEntityPreContext context)
    {
        var attacker = context.Params.Info.Attacker.ResolvePlayerFromHandle();

        if (attacker is not { IsValid: true }) return;

        var victim = context.Params.Entity.Address.FindPlayerByPawnAddress();

        if (victim is not { IsValid: true } || !victim.IsAlive || victim.PlayerPawn is not { } pawn) return;

        if (!IsZombieAttackingHuman(attacker, victim)) return;

        var activeWeapon = attacker.PlayerPawn?
            .WeaponServices?
            .ActiveWeapon
            .Value;

        if (!InfectionDamagePolicy.IsKnifeAttack(context.Params.Info.DamageType, activeWeapon?.DesignerName))
        {
            SuppressDamage(ref context);
            return;
        }

        var armor = pawn.ArmorValue;
        var aliveHumanCount = PlayerManager
            .GetAllAliveHumans()
            .Count();

        switch (InfectionDamagePolicy.ResolveKnifeHit(armor, aliveHumanCount))
        {
            case InfectionKnifeHitOutcome.AbsorbWithArmor:
            {
                var armorDamage = InfectionDamagePolicy.GetArmorDamage(
                    activeWeapon?.As<CCSWeaponBase>().WeaponMode ?? CSWeaponMode.Primary_Mode,
                    coreConfig.Value
                );
                var remainingArmor = InfectionDamagePolicy.CalculateRemainingArmor(armor, armorDamage);

                victim.SetArmor(remainingArmor);

                // Броня поглощает удар целиком: движок не должен отдельно
                // уменьшать здоровье или повторно пересчитывать броню.
                SuppressDamage(ref context);
                return;
            }

            case InfectionKnifeHitOutcome.Infect:
                SuppressDamage(ref context);
                PlayerManager.TryInfect(victim, attacker);
                return;

            case InfectionKnifeHitOutcome.DamageLastHuman:
                // Последний человек сражается до смерти: броня остаётся нетронутой,
                // а исходный урон полностью проходит в здоровье.
                context.Params.Info.DamageFlags |= TakeDamageFlags_t.DFLAG_IGNORE_ARMOR;
                return;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    protected override HookResult OnPlayerConnectedFull(EventPlayerConnectFull @event)
    {
        var player = @event.UserIdPlayer;

        if (player is null)
        {
            return HookResult.Continue;
        }

        // Если Pawn уже готов, игрок попадёт в раунд на следующем world update.
        // Если ещё нет, PlayerService повторит инициализацию после появления Pawn.
        Core.Scheduler.NextWorldUpdate(() => TryRespawnPlayer(player));

        return HookResult.Continue;
    }
    
    protected override HookResult OnPlayerTeam(EventPlayerTeam @event)
    {
        if (@event.Disconnect || @event.OldTeam != (byte)Team.Spectator || @event.Team != (byte)Team.T)
        {
            return HookResult.Continue;
        }

        var player = @event.UserIdPlayer;

        if (player is null)
        {
            return HookResult.Continue;
        }

        Core.Scheduler.NextWorldUpdate(() => TryRespawnPlayer(player));

        return HookResult.Continue;
    }

    protected override HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        var player = @event.UserIdPlayer;

        if (player is not { IsValid: true })
        {
            return HookResult.Continue;
        }

        if (PlayerManager.IsZombie(player) ||
            PlayerManager.IsHuman(player) &&
            PlayerManager.GetAllAliveHumans().Any() &&
            PlayerManager.TryInfect(player))
        {
            var steamId = GetPlayerIdentity(player);

            if (_respawnTracker.TryQueueRespawn(steamId, DateTimeOffset.UtcNow))
            {
                ScheduleZombieRespawn(player, steamId);
            }
        }

        return HookResult.Continue;
    }

    protected override Team? DetermineWinner()
    {
        var winner = base.DetermineWinner();

        // Последний живой зомби может уже быть мёртв, но ожидать разрешённого respawn.
        // В этом случае нельзя завершать раунд победой людей раньше таймера.
        if (winner == Team.CT && HasConnectedPendingRespawn())
        {
            return null;
        }

        return winner;
    }

    protected void PlayWinnerSound()
    {
        if (RoundWinner == null) return;
        
        if (RoundWinner == Team.T)
        {
            SoundExt.PlayGlobal(coreConfig.Value.ZombieWinSounds.GetRandomString(), coreConfig.Value.WinSoundVolume);
        }
        else if (RoundWinner == Team.CT)
        {
            SoundExt.PlayGlobal(coreConfig.Value.HumanWinSounds.GetRandomString(), coreConfig.Value.WinSoundVolume);
        }
    }

    protected void ClearRespawns()
    {
        var timers = _respawnTimers.Values.ToArray();
        _respawnTimers.Clear();

        foreach (var timer in timers)
        {
            timer.Cancel();
        }

        _respawnTracker.Clear();
    }
    
    public override bool TryRespawnPlayer(IPlayer player)
    {
        if (!player.IsValid)
        {
            return false;
        }

        if (!EnsureZombieRole(player))
        {
            return false;
        }

        // Уже живого late-join игрока достаточно перевести в роль зомби.
        if (player.IsAlive)
        {
            return true;
        }

        var steamId = GetPlayerIdentity(player);

        // Игрок умер после исчерпания лимита. Reconnect не должен дать ему новую жизнь.
        if (_respawnTracker.IsEliminated(steamId))
        {
            return false;
        }

        // Если игрок отключился между смертью и таймером, после reconnect продолжаем
        // тот же pending-respawn с исходным дедлайном и тем же SteamID-счётчиком.
        if (_respawnTracker.IsPending(steamId))
        {
            ScheduleZombieRespawn(player, steamId);
            return true;
        }

        // Первый вход в уже активный раунд или reconnect игрока, который отключился живым,
        // не расходует одну из автоматических жизней.
        return PlayerManager.TryRespawn(player);
    }

    private bool HasConnectedPendingRespawn()
    {
        var pending = _respawnTracker.GetPendingSteamIds().ToHashSet();

        if (pending.Count == 0)
        {
            return false;
        }

        foreach (var player in Core.PlayerManager.GetAllPlayers())
        {
            if (player is not { IsValid: true })
            {
                continue;
            }

            if (pending.Contains(GetPlayerIdentity(player)))
            {
                return true;
            }
        }

        return false;
    }

    private bool EnsureZombieRole(IPlayer player)
    {
        return PlayerManager.IsZombie(player) || PlayerManager.TryInfect(player);
    }

    private void ScheduleZombieRespawn(IPlayer player, ulong steamId, int retryAttempt = 0)
    {
        var remaining = _respawnTracker.GetRemainingDelay(steamId, DateTimeOffset.UtcNow);
        var delaySeconds = retryAttempt > 0
            ? RespawnRetryDelaySeconds
            : Math.Max(0.05f, (float)remaining.TotalSeconds);

        ScheduleZombieRespawn(
            steamId,
            player.PlayerID,
            player.SessionId,
            delaySeconds,
            retryAttempt
        );
    }

    private void ScheduleZombieRespawn(
        ulong steamId,
        int playerId,
        ulong sessionId,
        float delaySeconds,
        int retryAttempt)
    {
        CancelRespawnTimer(steamId);

        CancellationTokenSource? timer = null;
        timer = Core.Scheduler.DelayBySeconds(Math.Max(0.05f, delaySeconds), () =>
        {
            if (!_respawnTimers.TryGetValue(steamId, out var currentTimer) ||
                !ReferenceEquals(currentTimer, timer))
            {
                return;
            }

            _respawnTimers.Remove(steamId);
            TryAutomaticRespawn(steamId, playerId, sessionId, retryAttempt);
        });

        _respawnTimers[steamId] = timer;
    }

    private void TryAutomaticRespawn(ulong steamId, int playerId, ulong sessionId, int retryAttempt)
    {
        if (!_respawnTracker.IsPending(steamId))
        {
            return;
        }

        var player = Core.PlayerManager.GetPlayer(playerId);

        if (player is null || player.SessionId != sessionId)
        {
            // Игрок отключился. Pending-состояние остаётся по SteamID и будет
            // продолжено при reconnect, если текущий раунд ещё идёт.
            return;
        }

        if (!player.IsValid)
        {
            RetryAutomaticRespawn(steamId, playerId, sessionId, retryAttempt);
            return;
        }

        if (player.IsAlive)
        {
            // Например, администратор уже возродил игрока. Такой spawn не расходует лимит.
            _respawnTracker.CompleteExternalRespawn(steamId);
            return;
        }

        if (!EnsureZombieRole(player) || !PlayerManager.TryRespawn(player))
        {
            RetryAutomaticRespawn(steamId, playerId, sessionId, retryAttempt);
            return;
        }

        _respawnTracker.CompleteAutomaticRespawn(steamId);
    }

    private void RetryAutomaticRespawn(ulong steamId, int playerId, ulong sessionId, int retryAttempt)
    {
        if (retryAttempt >= MaxRespawnRetryAttempts)
        {
            _respawnTracker.FailPendingRespawn(steamId);
            TryRequestRoundEnd();
            return;
        }

        ScheduleZombieRespawn(
            steamId,
            playerId,
            sessionId,
            RespawnRetryDelaySeconds,
            retryAttempt + 1
        );
    }

    private void CancelRespawnTimer(ulong steamId)
    {
        if (_respawnTimers.Remove(steamId, out var timer))
        {
            timer.Cancel();
        }
    }

    private static ulong GetPlayerIdentity(IPlayer player)
    {
        var steamId = player.SteamID;
        return steamId != 0 ? steamId : player.UnauthorizedSteamID;
    }

    private bool IsZombieAttackingHuman(IPlayer attacker, IPlayer victim)
    {
        return PlayerManager.IsZombie(attacker) && PlayerManager.IsHuman(victim);
    }

    private static void SuppressDamage(ref TakeDamageEntityPreContext context)
    {
        context.Params.Info.Damage = 0;
        context.SetHookResult(HookResult.CancelOriginal);
    }
}
