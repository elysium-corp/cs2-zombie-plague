using System.Globalization;
using MapRotation.Core.Domain;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

namespace MapRotation.Core;

internal sealed class MapEngineAdapter(ISwiftlyCore core, TimeProvider clock) : IDisposable
{
    private static readonly string[] Limits = ["mp_timelimit", "mp_maxrounds", "mp_winlimit"];
    private static readonly string[] MatchEndControls = ["mp_match_end_changelevel", "mp_match_end_restart"];
    private readonly Dictionary<string, ConVarChange> _changes = [];
    private bool _disposed;
    private sealed record ConVarChange(string Original, string Requested, string? Applied, bool Restoring, DateTimeOffset QueuedAt);
    internal sealed record ConVarOverride(string Original, string Applied);
    internal sealed record PendingConVarChange(string Original, string Requested, bool Restoring);
    internal IReadOnlyDictionary<string, ConVarOverride> Overrides => _changes
        .Where(pair => !pair.Value.Restoring && pair.Value.Applied is not null)
        .ToDictionary(pair => pair.Key, pair => new ConVarOverride(pair.Value.Original, pair.Value.Applied!));
    internal IReadOnlyDictionary<string, PendingConVarChange> PendingConVars => _changes
        .Where(pair => pair.Value.Applied is null)
        .ToDictionary(pair => pair.Key, pair => new PendingConVarChange(pair.Value.Original, pair.Value.Requested, pair.Value.Restoring));
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
    public void ApplyRotationPolicy(bool rotationEnabled, bool mapLoaded = false)
    {
        if (_disposed) return;
        ObservePendingChanges();
        // Без пула карта остаётся бессрочной, но плагин не подавляет штатное
        // завершение матча, когда сам не может выполнить переход на другую карту.
        foreach (var name in Limits) SetZero(name, mapLoaded);
        foreach (var name in MatchEndControls)
            if (rotationEnabled) SetZero(name, mapLoaded);
            else Restore(name);
    }

    public void ObservePendingChanges()
    {
        if (_disposed) return;
        foreach (var (name, change) in _changes.ToArray())
        {
            if (change.Applied is not null) continue;
            var current = core.ConVar.FindAsString(name)?.ValueAsString;
            if (current is not null && ConVarCommand(name, current) == ConVarCommand(name, change.Requested))
            {
                if (change.Restoring) _changes.Remove(name);
                else _changes[name] = change with { Applied = current };
            }
            else if (clock.GetUtcNow() - change.QueuedAt >= TimeSpan.FromSeconds(5))
            {
                // Неподтверждённая запись не даёт права восстанавливать старое значение.
                _changes.Remove(name);
                core.Logger.LogWarning("[MapRotation] Команда ConVar {Name}={Requested} не подтверждена; текущее значение {Current}",
                    name, change.Requested, current);
            }
        }
    }

    private void SetZero(string name, bool mapLoaded)
    {
        var cvar = core.ConVar.FindAsString(name);
        if (cvar is null) return;
        var current = cvar.ValueAsString;
        if (_changes.TryGetValue(name, out var owned))
        {
            if (owned.Restoring)
            {
                // В очереди уже есть восстановление: новая политика должна идти после него,
                // даже если фактическое значение пока равно нулю.
                QueueValue(name, IsZero(current) ? owned.Original : current, "0", restoring: false);
                return;
            }
            if (current == owned.Applied) return;
            if (owned.Applied is null && !mapLoaded)
            {
                if (!IsZero(current) && current != owned.Original)
                    _changes[name] = owned with { Original = current };
                return;
            }
        }
        if (IsZero(current))
        {
            _changes.Remove(name);
            return;
        }

        QueueValue(name, current, "0", restoring: false);
    }

    private static bool IsZero(string value) =>
        bool.TryParse(value, out var boolean) ? !boolean :
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) && number == 0;

    internal static string ConVarCommand(string name, string value)
    {
        // В команду попадают только известные имена и заново отформатированные
        // числовые значения; исходная строка ConVar никогда не исполняется.
        var argument = name switch
        {
            "mp_timelimit" => FiniteFloat(value).ToString("R", CultureInfo.InvariantCulture),
            "mp_maxrounds" or "mp_winlimit" => int.Parse(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture),
            "mp_match_end_changelevel" or "mp_match_end_restart" => bool.TryParse(value, out var flag)
                ? flag ? "1" : "0" : int.Parse(value, CultureInfo.InvariantCulture) switch
                { 0 => "0", 1 => "1", _ => throw new FormatException("Некорректное логическое значение ConVar") },
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, "Неизвестный параметр ротации")
        };
        return name + " " + argument + "\n";
    }

    private static float FiniteFloat(string value)
    {
        var number = float.Parse(value, CultureInfo.InvariantCulture);
        return float.IsFinite(number) ? number : throw new FormatException("Неконечное значение ConVar");
    }

    private void QueueValue(string name, string original, string value, bool restoring)
    {
        // ServerCommand использует обработку команд движка. Ни строковый,
        // ни типизированный setter ConVar здесь не вызывается.
        core.Engine.ExecuteCommand(ConVarCommand(name, value));
        _changes[name] = new(original, value, null, restoring, clock.GetUtcNow());
    }

    private void Restore(string name)
    {
        if (!_changes.TryGetValue(name, out var owned) || owned.Restoring) return;
        var current = core.ConVar.FindAsString(name)?.ValueAsString;
        if (current is null || owned.Applied is not null && current != owned.Applied)
        {
            _changes.Remove(name);
            return;
        }
        // Если ноль ещё в очереди, компенсация обязана следовать за ним.
        // Внешнее ненулевое изменение сохраняется вместо прежней точки восстановления.
        var restore = owned.Applied is null && !IsZero(current) && current != owned.Original ? current : owned.Original;
        QueueValue(name, owned.Original, restore, restoring: true);
    }

    public void Dispose()
    {
        if (_disposed) return;
        foreach (var name in _changes.Keys.ToArray()) Restore(name);
        _changes.Clear();
        _disposed = true;
    }
}
