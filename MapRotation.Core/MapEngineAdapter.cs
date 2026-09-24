using System.Globalization;
using MapRotation.Core.Domain;
using SwiftlyS2.Shared;

namespace MapRotation.Core;

internal sealed class MapEngineAdapter(ISwiftlyCore core) : IDisposable
{
    private static readonly string[] Limits = ["mp_timelimit", "mp_maxrounds", "mp_winlimit"];
    private static readonly string[] MatchEndControls = ["mp_match_end_changelevel", "mp_match_end_restart"];
    private readonly Dictionary<string, ConVarOverride> _overrides = [];
    internal sealed record ConVarOverride(string Original, string Applied);
    internal IReadOnlyDictionary<string, ConVarOverride> Overrides => new Dictionary<string, ConVarOverride>(_overrides);
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
        // В SwiftlyS2 1.4.11 это единственный публичный Engine API смены карты.
        core.Engine.ExecuteCommand(Command(map));
    }
    public void ApplyRotationPolicy(bool rotationEnabled)
    {
        // Без пула карта остаётся бессрочной, но плагин не подавляет штатное
        // завершение матча, когда сам не может выполнить переход на другую карту.
        foreach (var name in Limits) SetZero(name);
        foreach (var name in MatchEndControls)
            if (rotationEnabled) SetZero(name);
            else Restore(name);
    }

    private void SetZero(string name)
    {
        var cvar = core.ConVar.FindAsString(name);
        if (cvar is null) return;
        var current = cvar.ValueAsString;
        if (_overrides.TryGetValue(name, out var owned) && current == owned.Applied) return;
        if (IsZero(current))
        {
            _overrides.Remove(name);
            return;
        }

        cvar.ValueAsString = "0";
        // Движок нормализует значения: bool возвращается как false, float —
        // например, как 0.000000. Сравниваем с фактически записанным значением.
        // Новый конфиг карты заменяет прежнюю точку восстановления.
        _overrides[name] = new(current, cvar.ValueAsString);
    }

    private static bool IsZero(string value) =>
        bool.TryParse(value, out var boolean) ? !boolean :
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && number == 0;

    private void Restore(string name)
    {
        if (!_overrides.TryGetValue(name, out var owned)) return;
        if (core.ConVar.FindAsString(name) is { } cvar && cvar.ValueAsString == owned.Applied)
            cvar.ValueAsString = owned.Original;
        _overrides.Remove(name);
    }

    public void Dispose()
    {
        foreach (var name in _overrides.Keys.ToArray()) Restore(name);
    }
}
