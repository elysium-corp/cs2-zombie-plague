using Common.Effects.Effects;
using Common.Hooks;
using Common.Hooks.Abstractions;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Api.Data;
using ZombiePlague.Api.Events.Contexts.Combat;
using ZombiePlague.Core.Config.Core;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Service.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Service;

internal sealed class KnockbackService(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    IOptions<ZombiePlagueCoreConfig> config,
    IHookPublisher hooks
) : IKnockbackService
{
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

    private bool _registered;

    // Горизонтальная скорость игрока до обработки урона от падения, по адресу pawn.
    private readonly Dictionary<nint, Vector> _fallVelocities = [];

    public void Register()
    {
        if (_registered)
        {
            return;
        }

        _registered = true;
        core.GameHooks.Entities.TakeDamage.Pre += OnTakeDamagePre;
        core.GameHooks.Entities.TakeDamage.Post += OnTakeDamagePost;
    }

    public void Unregister()
    {
        if (!_registered)
        {
            return;
        }

        core.GameHooks.Entities.TakeDamage.Pre -= OnTakeDamagePre;
        core.GameHooks.Entities.TakeDamage.Post -= OnTakeDamagePost;
        _fallVelocities.Clear();
        _registered = false;
    }

    public bool TryApplyKnockback(EventPlayerHurt @event, KnockbackData? knockbackData = null)
    {
        var victim = @event.UserIdPlayer;
        var attacker = @event.AttackerPlayer;

        if (victim is not { IsValid: true } ||
            attacker is not { IsValid: true })
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

        return TryApplyKnockback(
            attacker,
            victim,
            @event.ActualHitGroup == HitGroup_t.HITGROUP_HEAD,
            data
        );
    }

    private void OnTakeDamagePre(ref TakeDamageEntityPreContext context)
    {
        if (FallDamageMovement.IsFallDamage(context.Params.Info.DamageType))
        {
            PrepareFallDamage(ref context);
            return;
        }

        if (!config.Value.ZombieFriendlyKnockbackEnabled ||
            config.Value.ZombieFriendlyKnockbackForce <= 0.0f ||
            (context.Params.Info.DamageType & DamageTypes_t.DMG_SLASH) == 0)
        {
            return;
        }

        var attacker = context.Params.Info.Attacker.ResolvePlayerFromHandle();
        var victim = context.Params.Entity.Address.FindPlayerByPawnAddress();

        if (attacker is not { IsValid: true, IsAlive: true } ||
            victim is not { IsValid: true, IsAlive: true } ||
            attacker.PlayerID == victim.PlayerID ||
            !playerManager.IsZombie(attacker) ||
            !playerManager.IsZombie(victim) ||
            victim.IsFrozen())
        {
            return;
        }

        var activeWeapon = attacker.PlayerPawn?
            .WeaponServices?
            .ActiveWeapon
            .Value;

        if (activeWeapon is not { IsValidEntity: true } ||
            !activeWeapon.DesignerName.Contains("knife", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!TryCalculateFriendlyVelocity(
                attacker,
                victim,
                config.Value.ZombieFriendlyKnockbackForce,
                out var velocity
            ))
        {
            return;
        }

        TryApplyKnockback(
            attacker,
            victim,
            new KnockbackData(config.Value.ZombieFriendlyKnockbackForce, 0.0f),
            velocity
        );
    }

    // Урон от падения не должен сдвигать игрока: сила удара обнуляется, а горизонтальная
    // скорость после обработки урона возвращается к значению до него.
    // Сам урон и вертикальная скорость остаются штатными.
    private void PrepareFallDamage(ref TakeDamageEntityPreContext context)
    {
        var address = context.Params.Entity.Address;
        var victim = address.FindPlayerByPawnAddress();

        if (victim is not { IsValid: true, IsAlive: true } ||
            victim.PlayerPawn is not { IsValid: true } pawn)
        {
            _fallVelocities.Remove(address);
            return;
        }

        context.Params.Info.DamageForce = Vector.Zero;
        _fallVelocities[address] = pawn.AbsVelocity;
    }

    private void RestoreFallVelocity(ref TakeDamageEntityPostContext context)
    {
        var address = context.Params.Entity.Address;

        if (!_fallVelocities.Remove(address, out var before))
        {
            return;
        }

        var victim = address.FindPlayerByPawnAddress();

        if (victim is not { IsValid: true, IsAlive: true } ||
            victim.PlayerPawn is not { IsValid: true } pawn ||
            !FallDamageMovement.TryRestore(before, pawn.AbsVelocity, out var velocity))
        {
            return;
        }

        victim.Teleport(null, null, velocity);
    }

    private void OnTakeDamagePost(ref TakeDamageEntityPostContext context)
    {
        if (FallDamageMovement.IsFallDamage(context.Params.Info.DamageType))
        {
            RestoreFallVelocity(ref context);
            return;
        }

        if (!config.Value.KnockbackEnabled ||
            context.Params.Info.DamageType == DamageTypes_t.DMG_POISON ||
            (context.Params.Info.DamageType & DamageTypes_t.DMG_BURN) != 0 &&
            context.Params.Info.DamageCustom == Burn.DamageCustomId ||
            context.Params.Info.NumObjectsPenetrated > 0)
        {
            return;
        }

        var attacker = context.Params.Info.Attacker.ResolvePlayerFromHandle();
        var victim = context.Params.Entity.Address.FindPlayerByPawnAddress();

        if (attacker is not { IsValid: true } ||
            victim is not { IsValid: true })
        {
            return;
        }

        var activeWeapon = attacker.PlayerPawn?
            .WeaponServices?
            .ActiveWeapon
            .Value;

        if (activeWeapon is not { IsValidEntity: true })
        {
            return;
        }

        var weaponName = activeWeapon.DesignerName;

        if (weaponName.Contains("knife", StringComparison.OrdinalIgnoreCase))
        {
            weaponName = "weapon_knife";
        }

        if (!WeaponKnockback.TryGetValue(weaponName, out var data))
        {
            return;
        }

        TryApplyKnockback(
            attacker,
            victim,
            context.Params.Info.ActualHitGroup == HitGroup_t.HITGROUP_HEAD,
            data
        );
    }

    private bool TryApplyKnockback(
        IPlayer attacker,
        IPlayer victim,
        bool isHeadShot,
        KnockbackData data
    )
    {
        if (playerManager.IsZombie(attacker) ||
            !playerManager.TryGetZombie(victim, out var zombie) ||
            victim.IsFrozen())
        {
            return false;
        }

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

        return TryApplyKnockback(attacker, victim, data, velocity);
    }

    private bool TryApplyKnockback(
        IPlayer attacker,
        IPlayer victim,
        KnockbackData data,
        Vector velocity
    )
    {
        var preContext = new KnockbackApplyingContext(attacker, victim, data, velocity);

        if (!hooks.DispatchCancellable(ref preContext))
        {
            return false;
        }

        ApplyKnockback(victim, preContext.Velocity);

        var postContext = new KnockbackAppliedContext(
            attacker,
            victim,
            data,
            preContext.Velocity
        );

        hooks.Dispatch(ref postContext);

        return true;
    }

    private static bool TryCalculateFriendlyVelocity(
        IPlayer attacker,
        IPlayer victim,
        float force,
        out Vector velocity
    )
    {
        velocity = Vector.Zero;

        var attackerOrigin = attacker.PlayerPawn?.AbsOrigin;
        var victimPawn = victim.PlayerPawn;
        var victimOrigin = victimPawn?.AbsOrigin;

        if (attackerOrigin is null ||
            victimPawn is null ||
            victimOrigin is null ||
            force <= 0.0f)
        {
            return false;
        }

        var delta = victimOrigin.Value - attackerOrigin.Value;
        var length = MathF.Sqrt(delta.X * delta.X + delta.Y * delta.Y);

        if (length <= float.Epsilon)
        {
            return false;
        }

        var currentVelocity = victimPawn.AbsVelocity;
        var directionX = delta.X / length;
        var directionY = delta.Y / length;

        velocity = GetHorizontalKnockbackVelocity(
            currentVelocity,
            directionX,
            directionY,
            force
        );

        return true;
    }

    private void ApplyKnockback(IPlayer victim, Vector velocity)
    {
        var pawn = victim.PlayerPawn;

        if (pawn is not { IsValid: true })
        {
            return;
        }

        var currentVelocity = pawn.AbsVelocity;

        if (pawn.GroundEntity.Value is not null &&
            velocity.Z > currentVelocity.Z)
        {
            pawn.GroundEntity.Value = null;
        }

        pawn.AbsVelocity = velocity;
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

        var hitGroupMultiplier = isHeadShot
            ? config.Value.KnockbackHeadMultiply
            : config.Value.KnockbackBodyMultiply;
        var multiplier = recoil * zombieKnockback * hitGroupMultiplier;

        if (multiplier < config.Value.MinKnockbackForce)
        {
            return false;
        }

        var isOnGround = victimPawn.GroundEntity.Value is not null;
        var verticalBoost = isOnGround
            ? config.Value.GroundKnockback
            : config.Value.AirKnockback;
        var currentVelocity = victimPawn.AbsVelocity;
        var horizontalVelocity = GetHorizontalKnockbackVelocity(
            currentVelocity,
            direction.X,
            direction.Y,
            multiplier
        );
        
        velocity = new Vector(
            horizontalVelocity.X,
            horizontalVelocity.Y,
            currentVelocity.Z + verticalBoost
        );

        return true;
    }

    private static Vector GetHorizontalKnockbackVelocity(
        Vector currentVelocity,
        float directionX,
        float directionY,
        float force
    )
    {
        var velocityAlongDirection =
            currentVelocity.X * directionX +
            currentVelocity.Y * directionY;
        var opposingVelocity = MathF.Min(velocityAlongDirection, 0.0f);

        return new Vector(
            currentVelocity.X - directionX * opposingVelocity + directionX * force,
            currentVelocity.Y - directionY * opposingVelocity + directionY * force,
            currentVelocity.Z
        );
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

}
