using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameHooks;
using ZombiePlague.Core.Data.Managers;

namespace ZombiePlague.Core.Utils.Extensions;

internal static class TakeDamageEventExtensions
{
    public static void ApplyNemesisExtraDamage(
        this ref TakeDamageEntityPreContext @event,
        int extraDamage,
        IZombieManager zombieManager,
        ISwiftlyCore core)
    {
        var attacker = @event.Params.Info.Attacker.ResolvePlayerFromHandle(core);
        if (attacker == null || !attacker.IsValid)
        {
            return;
        }

        var victim = @event.Params.Entity.Address.FindPlayerByPawnAddress(core);
        if (victim == null || !victim.IsValid || victim.PlayerPawn is not { } pawn)
        {
            return;
        }

        if (zombieManager.IsNemesis(attacker))
            @event.Params.Info.Damage += extraDamage;
    }
}
