using ZPCore.Config.Ability;
using ZPCore.Data.Abilities.Contracts;
using ZPCore.Data.Extensions;
using ZPCore.Di;
using ZPCore.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;

namespace ZPCore.Data.Abilities;

internal class Leap(ISwiftlyCore core, LeapConfig config) : BaseActiveAbility(core)
{
    public override KeyKind? Key => KeyKind.Ctrl; 
    public override float Cooldown => config.CooldownTime;
    
    public override bool IsCooldownNotify => false;
    
    public override void Use()
    {
        var casterPawn = Caster.RequiredPlayerPawn;
        var viewAngles = casterPawn.EyeAngles;
        var forward = MathAlgorithm.ForwardFromAngles(viewAngles);
        
        var leapVelocity = forward * config.LeapDistance;
        leapVelocity.Z = config.LeapBoost * (1 / casterPawn.GravityScale );
        
        Caster.Teleport(casterPawn.AbsOrigin, viewAngles, leapVelocity);
        
        base.Use();
    }
    
    protected override bool CanUse()
    {
        if (!Caster.IsValid)
        {
            return false;
        }

        if (!Caster.IsAlive)
        {
            return false;
        }

        if (!Caster.IsInfected())
        {
            return false;
        }

        if ((Caster.PlayerPawn?.MovementServices?.Buttons.ButtonPressed & GameButtonFlags.Space) == 0)
        {
            return false;
        }
        
        var pawn = Caster.PlayerPawn;

        var deltaTime = core.Engine.GlobalVars.TickCount - Caster.RequiredPlayerPawn?.MovementServices?.LastJumpTick.Value;
  
        if (Caster.PlayerPawn?.GroundEntity.Value != null)
        {
            return false;
        }
        
        if (Caster.PlayerPawn?.GroundEntity.Value == null && deltaTime > 60)
        {
            return false;
        }
        

        return true;
    }
}