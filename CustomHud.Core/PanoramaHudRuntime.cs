using SwiftlyS2.Shared;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomHud.Core;

/// <summary>Операции над одной принадлежащей сервису сущностью, только на игровом потоке.</summary>
internal interface IHudRuntime : IDisposable
{
    /// <summary>Существует ли ещё сущность на текущей карте.</summary>
    bool IsValid { get; }
    /// <summary>Устанавливает или снимает класс панели для одного клиента.</summary>
    void SetClass(int playerId, string panel, string name, bool enabled);
    /// <summary>Заменяет текст динамической переменной value у одного клиента.</summary>
    void SetText(int playerId, string panel, string text);
}

internal sealed class PanoramaHudRuntime : IHudRuntime
{
    internal const string Layout = "panorama/layout/custom_game/elysium_messages_v4_r3.vxml_c";
    internal const string Style = "panorama/styles/custom_game/elysium_messages_v4_r3.vcss_c";
    internal const int RegionCount = 9;
    internal const int StackCapacity = 3;
    private readonly ISwiftlyCore _core;
    private readonly Dictionary<int, CCSCustomHudLayout> _entities = [];
    private bool _disposed;

    internal PanoramaHudRuntime(ISwiftlyCore core)
    {
        _core = core;
        var missing = MissingResources(path => core.GameFileSystem.FileExists(path, "GAME"));
        if (missing.Length > 0)
            throw new FileNotFoundException("Custom HUD: скомпилируйте и смонтируйте полный VPK v4 r3 у сервера и клиентов: " + string.Join(", ", missing));
    }

    internal static string[] MissingResources(Func<string, bool> exists) => new[] { Layout, Style }
        .Concat(new[] { "info", "warning", "infection", "skull", "shield", "trophy", "star", "gift", "megaphone", "lightning", "clock", "heart" }
            .Select(icon => $"panorama/images/custom_game/elysium/banners/{icon}.vsvg_c"))
        .Where(path => !exists(path)).ToArray();

    public bool IsValid => !_disposed && _entities.Values.All(entity => entity.IsValidEntity);

    // Каждая область получает отдельный ограниченный пул: 3 карточки вместо 27 в одной сущности.
    internal static (int Region, string Panel) Address(string panel)
    {
        const int prefix = 7;
        if (!panel.StartsWith("Message", StringComparison.Ordinal)) throw new ArgumentException("Неизвестная панель HUD", nameof(panel));
        var end = prefix;
        while (end < panel.Length && char.IsAsciiDigit(panel[end])) end++;
        if (!int.TryParse(panel.AsSpan(prefix, end - prefix), out var slot) || slot is < 0 or >= RegionCount * StackCapacity)
            throw new ArgumentException("Неизвестный слот HUD", nameof(panel));
        return (slot % RegionCount, $"Message{slot / RegionCount}{panel[end..]}");
    }

    private CCSCustomHudLayout Entity(int region)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_entities.TryGetValue(region, out var existing)) return existing;
        var entity = _core.EntitySystem.CreateEntity<CCSCustomHudLayout>();
        try
        {
            entity.StrLayout = Layout;
            entity.StrLayoutUpdated();
            entity.DispatchSpawn();
            entity.SetInputCaptureEnabled(false);
            entity.SetHasClass("MessageRegion", "Position" + (CustomHud.Api.HudPosition)region, EHudPanelClassStatus_t.k_eHudPanelClassStatus_HasClass);
            _entities.Add(region, entity);
            return entity;
        }
        catch { if (entity.IsValidEntity) entity.Despawn(); throw; }
    }

    public void SetClass(int playerId, string panel, string name, bool enabled)
    {
        var address = Address(panel);
        if (!enabled && !_entities.ContainsKey(address.Region)) return;
        Entity(address.Region).SetHasClassForPlayer(playerId, address.Panel, name, enabled
            ? EHudPanelClassStatus_t.k_eHudPanelClassStatus_HasClass
            : EHudPanelClassStatus_t.k_eHudPanelClassStatus_Undefined);
    }

    public void SetText(int playerId, string panel, string text)
    {
        var address = Address(panel);
        if (text.Length == 0 && !_entities.ContainsKey(address.Region)) return;
        var entity = Entity(address.Region);
        if (text.Length == 0) entity.RemoveDialogVariableStringForPlayer(playerId, address.Panel, "value");
        else entity.SetDialogVariableStringForPlayer(playerId, address.Panel, "value", text);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var entity in _entities.Values)
            if (entity.IsValidEntity) entity.Despawn();
        _entities.Clear();
    }
}
