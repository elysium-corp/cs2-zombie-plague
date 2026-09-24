using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Misc;

namespace MapRotation.Core;

/// <summary>Снимок движка по команде администратора; не изменяет игровые правила.</summary>
internal static class EngineStateDiagnostics
{
    private static readonly string[] ConVars =
    [
        "game_type", "game_mode", "host_timescale", "mp_restartgame",
        "mp_freezetime", "mp_roundtime", "mp_roundtime_defuse", "mp_roundtime_hostage",
        "mp_timelimit", "mp_maxrounds", "mp_winlimit", "mp_match_end_changelevel",
        "mp_match_end_restart", "mp_match_end_at_timelimit", "mp_match_restart_delay",
        "mp_ignore_round_win_conditions", "mp_warmup_pausetimer"
    ];

    internal static object Capture(ISwiftlyCore core, MapEngineAdapter maps) => new
    {
        CapturedAtUtc = DateTimeOffset.UtcNow,
        Globals = Read(() =>
        {
            var globals = core.Engine.GlobalVars;
            return new { globals.FrameCount, globals.TickCount, globals.CurrentTime, globals.FrameTime, globals.RealTime };
        }),
        Rules = Read(() =>
        {
            var rules = core.EntitySystem.GetGameRules();
            if (rules is null) return null;
            return new
            {
                rules.GamePhase,
                PhaseName = ((GamePhase)rules.GamePhase).ToString(),
                rules.GamePaused, rules.MatchWaitingForResume, rules.FreezePeriod, rules.WarmupPeriod,
                rules.TerroristTimeOutActive, rules.CTTimeOutActive, rules.TechnicalTimeOut,
                rules.HasMatchStarted, rules.GameRestart, rules.FreezeTime, rules.RoundTime,
                RoundStartTime = rules.RoundStartTime.Value,
                RestartRoundTime = rules.RestartRoundTime.Value,
                IntermissionStartTime = rules.IntermissionStartTime.Value,
                IntermissionEndTime = rules.IntermissionEndTime.Value,
                rules.EndMatchOnRoundReset, rules.EndMatchOnThink,
                rules.RoundWinStatus, rules.RoundWinReason,
                rules.NumTerrorist, rules.NumCT, rules.NumSpawnableTerrorist, rules.NumSpawnableCT
            };
        }),
        Players = Read(() => core.PlayerManager.GetAllPlayers()
            .Where(player => player.IsValid && !player.IsFakeClient)
            .Select(player => Read(() =>
            {
                var pawn = player.PlayerPawn;
                return new
                {
                    player.PlayerID, player.IsAlive,
                    Team = player.Controller.Team.ToString(),
                    Pawn = pawn is not { IsValid: true } ? null : new
                    {
                        pawn.Health, pawn.Flags,
                        MoveType = pawn.MoveType.ToString(),
                        ActualMoveType = pawn.ActualMoveType.ToString(),
                        pawn.VelocityModifier
                    }
                };
            })).ToArray()),
        ConVars = ConVars.ToDictionary(name => name, name => Read(() => core.ConVar.FindAsString(name)?.ValueAsString)),
        Overrides = maps.Overrides
    };

    private static object? Read(Func<object?> read)
    {
        try { return read(); }
        catch (Exception error) { return new { Error = error.Message }; }
    }
}
