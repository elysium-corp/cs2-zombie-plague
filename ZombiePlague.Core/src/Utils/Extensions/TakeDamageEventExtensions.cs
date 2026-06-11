using SwiftlyS2.Shared.GameHooks;

namespace ZombiePlague.Core.Utils.Extensions;

public static class TakeDamageEventExtensions
{
    public static void ApplyNemesisExtraDamage(this ref TakeDamageEntityPreContext @event, int extraDamage)
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

        if (attacker.IsNemesis())
            @event.Params.Info.Damage += extraDamage;
    }
}