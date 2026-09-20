using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace ZombiePlague.Core.Utils.Helpers;

internal static class PlayerGlowHelper
{
    public static readonly Color NemesisColor = new(255, 0, 0, 255);
    public static readonly Color SurvivorColor = new(0, 0, 255, 255);

    private const int GlowTypeOutline = 3;
    private const int GlowRange = 5000;

    public static void Apply(CCSPlayerPawn pawn, Color color)
    {
        var glow = pawn.Glow;

        if (!glow.IsValid)
        {
            return;
        }

        glow.GlowColorOverride = color;
        glow.GlowRange = GlowRange;
        glow.GlowRangeMin = 0;
        glow.GlowTeam = -1;
        glow.GlowType = GlowTypeOutline;

        glow.GlowColorOverrideUpdated();
        glow.GlowRangeUpdated();
        glow.GlowRangeMinUpdated();
        glow.GlowTeamUpdated();
        glow.GlowTypeUpdated();
    }

    public static void Clear(CCSPlayerPawn pawn)
    {
        var glow = pawn.Glow;

        if (!glow.IsValid || glow.GlowType == 0)
        {
            return;
        }

        glow.GlowRange = 0;
        glow.GlowRangeMin = 0;
        glow.GlowTeam = -1;
        glow.GlowType = 0;

        glow.GlowRangeUpdated();
        glow.GlowRangeMinUpdated();
        glow.GlowTeamUpdated();
        glow.GlowTypeUpdated();
    }
}
