using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;

namespace CS2ZombiePlague.Data;

public class Knockback(ISwiftlyCore core, ZombieManager zombieManager, KnifeManager knifeManager)
{
    private record KnockbackData(float Recoil, float PickDistance);

    private readonly Dictionary<string, KnockbackData> _weaponKnockback = new()
    {
        { "weapon_glock", new KnockbackData(150.0f, 200.0f) },
        { "weapon_usp_silencer", new KnockbackData(160.0f, 200.0f) },
        { "weapon_hkp2000", new KnockbackData(200.0f, 200.0f) },
        { "weapon_elite", new KnockbackData(225.0f, 200.0f) },
        { "weapon_p250", new KnockbackData(225.0f, 200.0f) },
        { "weapon_fiveseven", new KnockbackData(225.0f, 200.0f) },
        { "weapon_cz75a", new KnockbackData(270.0f, 200.0f) },
        { "weapon_deagle", new KnockbackData(900.0f, 125.0f) },
        { "weapon_revolver", new KnockbackData(900.0f, 125.0f) },
        { "weapon_nova", new KnockbackData(500.0f, 75.0f) },
        { "weapon_xm1014", new KnockbackData(500.0f, 75.0f) },
        { "weapon_sawedoff", new KnockbackData(500.0f, 75.0f) },
        { "weapon_mag7", new KnockbackData(500.0f, 75.0f) },
        { "weapon_m249", new KnockbackData(300.0f, 75.0f) },
        { "weapon_negev", new KnockbackData(300.0f, 150.0f) },
        { "weapon_mac10", new KnockbackData(300.0f, 150.0f) },
        { "weapon_mp7", new KnockbackData(300.0f, 150.0f) },
        { "weapon_mp9", new KnockbackData(300.0f, 150.0f) },
        { "weapon_mp5sd", new KnockbackData(300.0f, 150.0f) },
        { "weapon_ump45", new KnockbackData(300.0f, 150.0f) },
        { "weapon_p90", new KnockbackData(300.0f, 150.0f) },
        { "weapon_bizon", new KnockbackData(300.0f, 150.0f) },
        { "weapon_galilar", new KnockbackData(300.0f, 150.0f) },
        { "weapon_famas", new KnockbackData(300.0f, 150.0f) },
        { "weapon_ak47", new KnockbackData(600.0f, 150.0f) },
        { "weapon_m4a4", new KnockbackData(600.0f, 150.0f) },
        { "weapon_m4a1_silencer", new KnockbackData(350.0f, 150.0f) },
        { "weapon_ssg08", new KnockbackData(300.0f, 150.0f) },
        { "weapon_sg556", new KnockbackData(300.0f, 150.0f) },
        { "weapon_aug", new KnockbackData(300.0f, 150.0f) },
        { "weapon_awp", new KnockbackData(1200.0f, 400.0f) },
        { "weapon_g3sg1", new KnockbackData(300.0f, 150.0f) },
        { "weapon_scar20", new KnockbackData(300.0f, 150.0f) },
        { "weapon_knife", new KnockbackData(450.0f, 25.0f) },
    };

    public void Start()
    {
        core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurtPost);
    }
    
    private HookResult OnPlayerHurtPost(EventPlayerHurt @event)
    {
        var victim = core.PlayerManager.GetPlayer(@event.UserId);
        var attacker = core.PlayerManager.GetPlayer(@event.Attacker);
        if (victim == null || attacker == null)
        {
            return HookResult.Continue;
        }
        
        if (!victim.IsInfected() || attacker.IsInfected() ||  victim.IsFrozen())
        {
            return HookResult.Continue;
        }

        var weaponName = $"weapon_{@event.Weapon}";
        
        if (!_weaponKnockback.ContainsKey(weaponName))
        {
            return  HookResult.Continue;
        }
        
        var weaponKnockback = _weaponKnockback[weaponName].Recoil;
        var weaponPickDistance = _weaponKnockback[weaponName].PickDistance;
        
        if (weaponName == "weapon_knife")
        {
            weaponKnockback = knifeManager.GetCurrentKnife(attacker.PlayerID).Knockback;
        }

        var attackerPawn = attacker.RequiredPlayerPawn;
        var victimPawn = victim.RequiredPlayerPawn;
        
        var victimOrigin = victimPawn.AbsOrigin!.Value;
        var attackerOrigin = attackerPawn.AbsOrigin!.Value;

        var directionVector = (victimOrigin - attackerOrigin).Normalized();
        var distance = GetDistance(victimOrigin, attackerOrigin);

        float recoil = GetGunRecoil(distance, weaponKnockback,
            weaponPickDistance);

        var zombie = zombieManager.GetZombie(victim.PlayerID);
        var zombieKnockback = zombie.GetZombieClass().Knockback;
        
        bool onGround = victimPawn.GroundEntity.Value != null;
        
        var zBoost = onGround ? 150f : 25f;

        var victimVelocity = victimPawn.AbsVelocity;
        
        Vector newVelocity = new Vector(
            victimVelocity.X + directionVector.X * recoil * zombieKnockback,
            victimVelocity.Y + directionVector.Y * recoil * zombieKnockback,
            victimVelocity.Z + zBoost
        );

        victimPawn.GroundEntity.Value = null;
        
        victim.Teleport(victimOrigin, victimPawn.EyeAngles, newVelocity);

        core.Scheduler.Delay(20, () => { victim.SetSpeed(zombie.GetZombieClass().Speed); });

        return HookResult.Continue;
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