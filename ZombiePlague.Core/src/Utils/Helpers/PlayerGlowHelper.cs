using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace ZombiePlague.Core.Utils.Helpers;

internal static class PlayerGlowHelper
{
    public static readonly Color NemesisColor = new(255, 0, 0, 255);
    public static readonly Color SurvivorColor = new(0, 0, 255, 255);

    private const int GlowTypeOutline = 3;
    private const int GlowRange = 5000;
    private const uint BoneMergeSpawnFlag = 256u;
    private const string GlowEntityDesignerName = "prop_dynamic";

    private static readonly Dictionary<int, PlayerGlowState> ActiveGlows = [];

    public static void Apply(ISwiftlyCore core, IPlayer player, Color color)
    {
        if (!player.IsValid || !player.IsAlive || player.PlayerPawn is not { IsValid: true } pawn)
        {
            return;
        }

        var modelName = pawn.GetModel();

        if (string.IsNullOrWhiteSpace(modelName))
        {
            return;
        }

        Clear(player.PlayerID);

        var relay = core.EntitySystem.CreateEntityByDesignerName<CDynamicProp>(GlowEntityDesignerName);

        if (!relay.IsValid)
        {
            return;
        }

        var glowEntity = core.EntitySystem.CreateEntityByDesignerName<CDynamicProp>(GlowEntityDesignerName);

        if (!glowEntity.IsValid)
        {
            if (relay.IsValidEntity)
            {
                relay.Despawn();
            }

            return;
        }

        relay.SetModel(modelName);
        relay.Spawnflags = BoneMergeSpawnFlag;
        relay.RenderMode = RenderMode_t.kRenderNone;
        relay.DispatchSpawn();

        glowEntity.SetModel(modelName);
        glowEntity.Spawnflags = BoneMergeSpawnFlag;
        glowEntity.RenderMode = RenderMode_t.kRenderTransAlpha;
        glowEntity.Render = new Color(255, 255, 255, 1);
        glowEntity.DispatchSpawn();

        var glow = glowEntity.Glow;

        if (!glow.IsValid)
        {
            if (glowEntity.IsValidEntity)
            {
                glowEntity.Despawn();
            }

            if (relay.IsValidEntity)
            {
                relay.Despawn();
            }

            return;
        }

        glow.GlowColorOverride = color;
        glow.GlowRange = GlowRange;
        glow.GlowRangeMin = 0;
        glow.GlowTeam = -1;
        glow.GlowType = GlowTypeOutline;
        glow.EligibleForScreenHighlight = true;
        glow.Glowing = true;

        glow.GlowColorOverrideUpdated();
        glow.GlowRangeUpdated();
        glow.GlowRangeMinUpdated();
        glow.GlowTeamUpdated();
        glow.GlowTypeUpdated();
        glow.EligibleForScreenHighlightUpdated();

        relay.AcceptInput("FollowEntity", "!activator", pawn, relay);
        glowEntity.AcceptInput("FollowEntity", "!activator", relay, glowEntity);

        ActiveGlows[player.PlayerID] = new PlayerGlowState(
            player.SessionId,
            core.EntitySystem.GetRefEHandle(glowEntity),
            core.EntitySystem.GetRefEHandle(relay)
        );
    }

    public static void Clear(IPlayer player)
    {
        if (
            !ActiveGlows.TryGetValue(player.PlayerID, out var state) ||
            state.SessionId != player.SessionId
        )
        {
            return;
        }

        Clear(player.PlayerID);
    }

    private static void Clear(int playerId)
    {
        if (!ActiveGlows.Remove(playerId, out var state))
        {
            return;
        }

        Despawn(state.Glow);
        Despawn(state.Relay);
    }

    private static void Despawn(CHandle<CDynamicProp> handle)
    {
        if (!handle.IsValid || handle.Value is not { IsValidEntity: true } entity)
        {
            return;
        }

        entity.Despawn();
    }

    private readonly record struct PlayerGlowState(
        ulong SessionId,
        CHandle<CDynamicProp> Glow,
        CHandle<CDynamicProp> Relay
    );
}
