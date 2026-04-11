using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Services;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;

namespace CustomEquipment.Controllers;

internal sealed class ParticleController(ISwiftlyCore core, IEquipmentService equipmentService/*, IParticleService particleService*/) : IParticleController, IDisposable
{
    private Guid _guidBulletImpactPost = Guid.Empty;
    
    public void Initialize()
    {
        _guidBulletImpactPost = core.GameEvent.HookPost<EventBulletImpact>(OnBulletImpactPost);
    }

    public void Dispose()
    {
        core.GameEvent.Unhook(_guidBulletImpactPost);
    }

    private HookResult OnBulletImpactPost(EventBulletImpact hook)
    {
        var attacker = hook.UserIdPlayer;

        if (attacker == null || !attacker.IsValid) return HookResult.Continue;

        var activeWeapon = equipmentService.GetActiveWeapon<BaseWeapon>(attacker);
        
        if (activeWeapon?.Particle != null && activeWeapon.HasTraceParticle())
        {
            var particleService = new ParticleService(core);
            var position = new Vector(hook.X, hook.Y, hook.Z);

            particleService.CreateTracerParticle(activeWeapon.Particle.Trace, activeWeapon.AttachedWeapon, position);
        }
        
        return HookResult.Continue;
    }
}