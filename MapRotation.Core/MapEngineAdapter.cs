using System.Globalization;
using MapRotation.Core.Domain;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace MapRotation.Core;

internal sealed class MapEngineAdapter(ISwiftlyCore core)
{
    private readonly Func<long, WorkshopInstallation> _readWorkshop = WorkshopMapFiles.Steam.Read;

    internal MapEngineAdapter(ISwiftlyCore core, Func<long, WorkshopInstallation> readWorkshop) : this(core)
        => _readWorkshop = readWorkshop;

    public string CurrentMap => core.Engine.GlobalVars.MapName.ToString();
    public string WorkshopId => core.Engine.WorkshopId;
    public bool IsValid(RotationMap map) => Inspect(map).IsValid;

    /// <summary>Проверяет карту без загрузки ресурсов и возвращает источник подтверждения доступности.</summary>
    public MapAvailability Inspect(RotationMap map)
    {
        if (!map.IsSafe) return new(false, "UnsafeMap", map.EngineTarget);
        if (core.Engine.IsMapValid(map.EngineTarget)) return new(true, "EngineTarget", map.EngineTarget);
        if (map.WorkshopId is not { } workshopId) return new(false, "Unavailable", map.EngineTarget);

        // Проверяем путь с тем же Workshop ID, который затем получит host_workshop_map.
        // Одного совпадения короткого имени недостаточно: оно может принадлежать другой карте.
        var workshopPath = $"workshop/{map.EngineTarget}/{map.MapName.Split('/')[^1]}";
        if (core.Engine.IsMapValid(workshopPath)) return new(true, "WorkshopMapPath", map.EngineTarget, workshopPath);

        // IsMapValid(ID) ищет VPK только под EXECUTABLE_PATH; Steam знает фактическую папку установки.
        var installation = _readWorkshop(workshopId);
        return new(installation.IsReady, installation.IsReady ? "SteamUGC" : "Unavailable",
            map.EngineTarget, workshopPath, installation);
    }

    internal static string Command(RotationMap map)
    {
        if (!map.IsSafe) throw new ArgumentException("Некорректная карта", nameof(map));
        return map.WorkshopId.HasValue ? "host_workshop_map " + map.EngineTarget : "changelevel " + map.MapName;
    }
    public void Change(RotationMap map)
    {
        if (!map.Enabled || !IsValid(map)) throw new InvalidOperationException("Карта отсутствует на сервере: " + map.Key);
        // В SwiftlyS2 1.4.12-beta.3 это единственный публичный Engine API смены карты.
        core.Engine.ExecuteCommand(Command(map));
    }

    public NativeMatchProgress? ReadMatchProgress()
    {
        var rules = core.EntitySystem.GetGameRules();
        if (rules is null) return null;
        var ended = rules.GamePhase == (int)GamePhase.GAMEPHASE_MATCH_ENDED;
        if (rules.WarmupPeriod || !rules.HasMatchStarted || ended)
            return new(rules.WarmupPeriod, rules.HasMatchStarted, ended, null, null);

        var maxRounds = (int)ReadNumber("mp_maxrounds");
        var winLimit = (int)ReadNumber("mp_winlimit");
        var canClinch = core.ConVar.FindAsString("mp_match_can_clinch")?.ValueAsString is "true" or "1";
        var highestScore = winLimit > 0 || maxRounds > 0 && canClinch
            ? core.EntitySystem.GetAllEntitiesByClass<CCSTeam>()
                .Where(team => team.IsValid && team.TeamNum is 2 or 3)
                .Select(team => team.Score).DefaultIfEmpty().Max() : 0;
        return new(false, true, false,
            NativeMatchProgress.RemainingSeconds(ReadNumber("mp_timelimit"),
                core.Engine.GlobalVars.CurrentTime, rules.GameStartTime),
            NativeMatchProgress.RemainingRounds(maxRounds, rules.TotalRoundsPlayed, canClinch, winLimit, highestScore));
    }

    private double ReadNumber(string name) =>
        double.TryParse(core.ConVar.FindAsString(name)?.ValueAsString, NumberStyles.Float,
            CultureInfo.InvariantCulture, out var number) && double.IsFinite(number) && number >= 0 && number <= int.MaxValue
            ? number : 0;
}

internal sealed record MapAvailability(bool IsValid, string Source, string EngineTarget,
    string? WorkshopMapPath = null, WorkshopInstallation? Workshop = null);
