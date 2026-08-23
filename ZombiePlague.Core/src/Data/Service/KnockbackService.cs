using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Api.Data;
using ZombiePlague.Core.Config.Core;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Service.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Service;

internal sealed class KnockbackService(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    IOptions<ZombiePlagueCoreConfig> config
) : IKnockbackService
{
    private const int SpeedRestoreDelay = 20;

    private static readonly IReadOnlyDictionary<string, KnockbackData> WeaponKnockback =
        new Dictionary<string, KnockbackData>
        {
            { "weapon_glock", new KnockbackData(200.0f, 200.0f) },
            { "weapon_usp_silencer", new KnockbackData(200.0f, 200.0f) },
            { "weapon_hkp2000", new KnockbackData(200.0f, 200.0f) },
            { "weapon_elite", new KnockbackData(225.0f, 200.0f) },
            { "weapon_p250", new KnockbackData(225.0f, 200.0f) },
            { "weapon_fiveseven", new KnockbackData(225.0f, 200.0f) },
            { "weapon_cz75a", new KnockbackData(270.0f, 200.0f) },
            { "weapon_deagle", new KnockbackData(650.0f, 125.0f) },
            { "weapon_revolver", new KnockbackData(500.0f, 125.0f) },
            { "weapon_nova", new KnockbackData(400.0f, 75.0f) },
            { "weapon_xm1014", new KnockbackData(400.0f, 75.0f) },
            { "weapon_sawedoff", new KnockbackData(400.0f, 75.0f) },
            { "weapon_mag7", new KnockbackData(500.0f, 75.0f) },
            { "weapon_m249", new KnockbackData(225.0f, 75.0f) },
            { "weapon_negev", new KnockbackData(225.0f, 125.0f) },
            { "weapon_mac10", new KnockbackData(225.0f, 125.0f) },
            { "weapon_mp7", new KnockbackData(225.0f, 125.0f) },
            { "weapon_mp9", new KnockbackData(225.0f, 125.0f) },
            { "weapon_mp5sd", new KnockbackData(225.0f, 125.0f) },
            { "weapon_ump45", new KnockbackData(225.0f, 125.0f) },
            { "weapon_p90", new KnockbackData(225.0f, 125.0f) },
            { "weapon_bizon", new KnockbackData(225.0f, 125.0f) },
            { "weapon_galilar", new KnockbackData(225.0f, 125.0f) },
            { "weapon_famas", new KnockbackData(225.0f, 125.0f) },
            { "weapon_ak47", new KnockbackData(225.0f, 125.0f) },
            { "weapon_m4a4", new KnockbackData(225.0f, 125.0f) },
            { "weapon_m4a1", new KnockbackData(225.0f, 125.0f) },
            { "weapon_m4a1_silencer", new KnockbackData(350.0f, 150.0f) },
            { "weapon_ssg08", new KnockbackData(225.0f, 150.0f) },
            { "weapon_sg556", new KnockbackData(225.0f, 150.0f) },
            { "weapon_aug", new KnockbackData(225.0f, 150.0f) },
            { "weapon_awp", new KnockbackData(1200.0f, 400.0f) },
            { "weapon_g3sg1", new KnockbackData(225.0f, 150.0f) },
            { "weapon_scar20", new KnockbackData(225.0f, 150.0f) },
            { "weapon_knife", new KnockbackData(450.0f, 25.0f) }
        };

    private readonly Dictionary<int, CancellationTokenSource> _speedRestoreTimers = [];

    private Guid _playerHurtHook = Guid.Empty;

    public void Register()
    {
        if (!config.Value.KnockbackEnabled || _playerHurtHook != Guid.Empty)
        {
            return;
        }

        _playerHurtHook = core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurtPost);
    }

    public void Unregister()
    {
        if (_playerHurtHook != Guid.Empty)
        {
            core.GameEvent.Unhook(_playerHurtHook);
            _playerHurtHook = Guid.Empty;
        }

        CancelSpeedRestoreTimers();
    }

    public bool TryApplyKnockback(EventPlayerHurt @event, KnockbackData? knockbackData = null)
    {
        var victim = @event.UserIdPlayer;
        var attacker = @event.AttackerPlayer;

        if (
            victim is not { IsValid: true } ||
            attacker is not { IsValid: true } ||
            playerManager.IsZombie(attacker) ||
            !playerManager.TryGetZombie(victim, out var zombie) ||
            victim.IsFrozen()
        )
        {
            return false;
        }

        var data = knockbackData;

        if (data is null)
        {
            var weaponName = $"weapon_{@event.Weapon}";

            if (!WeaponKnockback.TryGetValue(weaponName, out data))
            {
                return false;
            }
        }

        var isHeadShot = @event.ActualHitGroup == HitGroup_t.HITGROUP_HEAD;

        if (!TryCalculateVelocity(
                attacker,
                victim,
                isHeadShot,
                data,
                zombie.ZClass.Knockback,
                out var velocity
            ))
        {
            return false;
        }

        ApplyKnockback(victim, velocity);

        return true;
    }

    private HookResult OnPlayerHurtPost(EventPlayerHurt @event)
    {
        TryApplyKnockback(@event);

        return HookResult.Continue;
    }

    private void ApplyKnockback(IPlayer victim, Vector velocity)
    {
        victim.Teleport(null, null, velocity);

        var playerId = victim.PlayerID;

        CancelSpeedRestoreTimer(playerId);

        _speedRestoreTimers[playerId] = core.Scheduler.Delay(
            SpeedRestoreDelay,
            () =>
            {
                _speedRestoreTimers.Remove(playerId);

                if (
                    victim is { IsValid: true, IsAlive: true } &&
                    victim.PlayerID == playerId &&
                    playerManager.TryGetZombie(victim, out var zombie)
                )
                {
                    victim.SetSpeed(zombie.ZClass.Speed);
                }
            }
        );
    }

    private bool TryCalculateVelocity(
        IPlayer attacker,
        IPlayer victim,
        bool isHeadShot,
        KnockbackData knockbackData,
        float zombieKnockback,
        out Vector velocity
    )
    {
        velocity = Vector.Zero;

        var attackerPawn = attacker.PlayerPawn;
        var victimPawn = victim.PlayerPawn;

        if (
            attackerPawn?.AbsOrigin is null ||
            victimPawn?.AbsOrigin is null
        )
        {
            return false;
        }

        var attackerOrigin = attackerPawn.AbsOrigin.Value;
        var victimOrigin = victimPawn.AbsOrigin.Value;
        var direction = (victimOrigin - attackerOrigin).Normalized2D();
        var distance = GetDistance(victimOrigin, attackerOrigin);
        var recoil = GetWeaponRecoil(
            distance,
            knockbackData.Recoil,
            knockbackData.PickDistance
        );

        if (recoil < config.Value.MinKnockbackForce)
        {
            return false;
        }

        var isOnGround = victimPawn.GroundEntity.Value is not null;
        var verticalBoost = isOnGround
            ? config.Value.GroundKnockback
            : config.Value.AirKnockback;
        var hitGroupMultiplier = isHeadShot
            ? config.Value.KnockbackHeadMultiply
            : config.Value.KnockbackBodyMultiply;
        
        var currentVelocity = victimPawn.AbsVelocity;
        var multiplier = recoil * zombieKnockback * hitGroupMultiplier;
        
        velocity = new Vector(
            currentVelocity.X + direction.X * multiplier,
            currentVelocity.Y + direction.Y * multiplier,
            currentVelocity.Z + verticalBoost
        );

        return true;
    }

    private static float GetDistance(Vector first, Vector second)
    {
        var delta = first - second;

        return MathF.Sqrt(
            delta.X * delta.X +
            delta.Y * delta.Y +
            delta.Z * delta.Z
        );
    }

    private static float GetWeaponRecoil(
        float distance,
        float maxRecoil,
        float peakDistance,
        float decay = -0.002f
    )
    {
        return distance <= peakDistance
            ? maxRecoil
            : (float)(maxRecoil * Math.Exp(decay * (distance - peakDistance)));
    }

    private void CancelSpeedRestoreTimer(int playerId)
    {
        if (!_speedRestoreTimers.Remove(playerId, out var timer))
        {
            return;
        }

        timer.Cancel();
    }

    private void CancelSpeedRestoreTimers()
    {
        var timers = _speedRestoreTimers.Values.ToArray();
        _speedRestoreTimers.Clear();

        foreach (var timer in timers)
        {
            timer.Cancel();
        }
    }
}
