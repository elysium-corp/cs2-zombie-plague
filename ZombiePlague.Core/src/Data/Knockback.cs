using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Api.Data;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Utils.Extensions;
using ZPCore.Config.Core;

namespace ZombiePlague.Core.Data;

internal class Knockback(ISwiftlyCore core, ZombieManager zombieManager, IOptions<ZombiePlagueCoreConfig> config)
{
    private readonly Dictionary<string, KnockbackData> _weaponKnockback = new()
    {
        { "weapon_glock", new KnockbackData(150.0f, 200.0f) },
        { "weapon_usp_silencer", new KnockbackData(160.0f, 200.0f) },
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
        { "weapon_p90", new KnockbackData(225.0f, 125) },
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
        { "weapon_knife", new KnockbackData(450.0f, 25.0f) },
    };

    public void Start()
    {
        core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurtPost);
    }
    
    private HookResult OnPlayerHurtPost(EventPlayerHurt @event)
    {
        TryApplyKnockback(@event, null);
        
        return HookResult.Continue;
    }

    public bool TryApplyKnockback(EventPlayerHurt @event, KnockbackData? knockbackData)
    {
        var victim = @event.UserIdPlayer;
        var attacker = @event.AttackerPlayer;   
        
        if (victim == null || attacker == null)
        {
            return false;
        }

        if (attacker.IsInfected())
        {
            return false;
        }
        
        if (!victim.IsInfected() || victim.IsFrozen())
        {
            return false;
        }

        var isHeadShot = @event.HitGroup == (int)HitGroup_t.HITGROUP_HEAD;

        if (knockbackData != null)
        {
            var newRecoil = CalculateRecoil(attacker, victim, isHeadShot, knockbackData);
            
            ApplyKnockback(victim, newRecoil);
            
            return true;
        }
        
        var weaponName = $"weapon_{@event.Weapon}";
        
        if (!_weaponKnockback.TryGetValue(weaponName, out var data))
        {
            return false;
        }
        
        var recoil = CalculateRecoil(attacker, victim, isHeadShot, data);
        
        ApplyKnockback(victim, recoil);

        return true;
    }

    private void ApplyKnockback(IPlayer victim, Vector newVelocity)
    {
        victim.Teleport(null, null, newVelocity);

        if (!victim.IsInfected())
        {
            return;
        }
        
        var zombie = zombieManager.GetZombie(victim.PlayerID);
            
        core.Scheduler.Delay(20, () =>
        {
            if (zombie != null)
            {
                victim.SetSpeed(zombie.ZClass.Speed);
            }
        }); 

    }

    private Vector CalculateRecoil(IPlayer attacker, IPlayer victim, bool isHeadShot, KnockbackData knockbackData)
    {
        var weaponKnockback = knockbackData.Recoil;
        var weaponPickDistance = knockbackData.PickDistance;

        var attackerPawn = attacker.PlayerPawn;
        var victimPawn = victim.PlayerPawn;

        if (attackerPawn == null || victimPawn == null)
        {
            return Vector.Zero;
        }
        
        var victimOrigin = victimPawn.AbsOrigin!.Value;
        var attackerOrigin = attackerPawn.AbsOrigin!.Value;

        var directionVector = (victimOrigin - attackerOrigin).Normalized();
        var distance = GetDistance(victimOrigin, attackerOrigin);

        float recoil = GetGunRecoil(distance, weaponKnockback,
            weaponPickDistance);

        if (recoil < config.Value.MinKnockbackForce)
        {
            return Vector.Zero;
        }
        
        var zombie = zombieManager.GetZombie(victim.PlayerID);
        if (zombie == null)
        {
            return Vector.Zero;
        }
        
        var zombieKnockback = zombie.ZClass.Knockback;
        var onGround = victimPawn.GroundEntity.Value != null;
        var zBoost = onGround ? config.Value.GroundKnockback : config.Value.AirKnockback;
        var victimVelocity = victimPawn.AbsVelocity;

        var hitGroupKnockback = isHeadShot ? config.Value.KnockbackHeadMultiply : config.Value.KnockbackBodyMultiply;
        
        return new Vector(
            victimVelocity.X + directionVector.X * recoil * zombieKnockback * hitGroupKnockback,
            victimVelocity.Y + directionVector.Y * recoil * zombieKnockback * hitGroupKnockback,
            victimVelocity.Z + zBoost
        );
    }
    
    private float GetDistance(Vector vector1, Vector vector2)
    {
        return (float)Math.Sqrt(Math.Pow(vector1.X - vector2.X, 2) + Math.Pow(vector1.Y - vector2.Y, 2) +
                                Math.Pow(vector1.Z - vector2.Z, 2));
    }

    private float GetGunRecoil(float distance, float recoilMax, float peakDistance, float k = -0.002f)
    {
        if (distance <= peakDistance)
            return recoilMax;
        return (float)(recoilMax * Math.Exp(k * (distance - peakDistance)));
    }
}