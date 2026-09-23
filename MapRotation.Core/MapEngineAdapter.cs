using MapRotation.Core.Domain;
using SwiftlyS2.Shared;

namespace MapRotation.Core;

internal sealed class MapEngineAdapter(ISwiftlyCore core) : IDisposable
{
    private readonly Dictionary<string, string> _original = [];
    public string CurrentMap => core.Engine.GlobalVars.MapName.ToString();
    public string WorkshopId => core.Engine.WorkshopId;
    public bool IsValid(RotationMap map) => map.IsSafe && core.Engine.IsMapValid(map.EngineTarget);

    internal static string Command(RotationMap map)
    {
        if (!map.IsSafe) throw new ArgumentException("Некорректная карта", nameof(map));
        return map.WorkshopId.HasValue ? "host_workshop_map " + map.EngineTarget : "changelevel " + map.MapName;
    }
    public void Change(RotationMap map)
    {
        if (!map.Enabled || !IsValid(map)) throw new InvalidOperationException("Карта отсутствует на сервере: " + map.Key);
        // В SwiftlyS2 1.4.11-beta.9 это единственный публичный Engine API смены карты.
        core.Engine.ExecuteCommand(Command(map));
    }
    public void OwnRotation()
    {
        foreach (var name in new[] { "mp_timelimit", "mp_maxrounds", "mp_winlimit", "mp_match_end_changelevel", "mp_match_end_restart" })
        {
            var cvar = core.ConVar.FindAsString(name);
            if (cvar is null) continue;
            _original.TryAdd(name, cvar.ValueAsString);
            if (cvar.ValueAsString != "0") cvar.ValueAsString = "0";
        }
    }
    public void Dispose()
    {
        foreach (var (name, value) in _original)
            if (core.ConVar.FindAsString(name) is { ValueAsString: "0" } cvar) cvar.ValueAsString = value;
        _original.Clear();
    }
}
