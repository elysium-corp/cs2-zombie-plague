using SwiftlyS2.Shared;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Shop.Core.Hud;

/// <summary>Персональная сущность HUD с отправкой только изменившихся значений, на игровом потоке.</summary>
internal interface IShopHudRuntime : IDisposable
{
    /// <summary>Существование сущности на текущей карте.</summary>
    bool IsValid { get; }
    /// <summary>Проверяет принадлежность события клика этой сущности.</summary>
    bool Owns(CCSCustomHudLayout entity);
    /// <summary>Обновляет строку value указанной панели.</summary>
    void Text(string panel, string value);
    /// <summary>Устанавливает персональное состояние класса.</summary>
    void Class(string panel, string name, bool enabled);
    /// <summary>Включает или выключает курсор только для владельца.</summary>
    void Capture(bool enabled);
}

internal sealed class ShopHudRuntime : IShopHudRuntime
{
    internal const string Layout = "panorama/layout/custom_game/elysium_shop_v4_r5.vxml_c";
    internal const string Style = "panorama/styles/custom_game/elysium_shop_v4_r5.vcss_c";
    internal const string IconsStyle = "panorama/styles/custom_game/elysium_equipment_icons_v4.vcss_c";
    internal const string SettingsIcon = "panorama/images/custom_game/shop/gear.vsvg_c";
    private readonly CCSCustomHudLayout _entity;
    private readonly int _playerId;
    private readonly Dictionary<string, string> _text = [];
    private readonly Dictionary<(string, string), bool> _classes = [];
    private bool _disposed;
    private bool _capture;

    public ShopHudRuntime(ISwiftlyCore core, int playerId)
    {
        foreach (var path in new[] { Layout, Style, IconsStyle, SettingsIcon })
            if (!core.GameFileSystem.FileExists(path, "GAME"))
                throw new FileNotFoundException("Shop HUD: отсутствует ресурс " + path);

        _playerId = playerId;
        _entity = core.EntitySystem.CreateEntity<CCSCustomHudLayout>();
        try
        {
            _entity.StrLayout = Layout;
            _entity.StrLayoutUpdated();
            _entity.DispatchSpawn();
            _entity.SetInputCaptureEnabled(false);
        }
        catch
        {
            if (_entity.IsValidEntity) _entity.Despawn();
            throw;
        }
    }

    public bool IsValid => !_disposed && _entity.IsValidEntity;
    public bool Owns(CCSCustomHudLayout entity) => IsValid && entity.IsValidEntity && entity.Address == _entity.Address;

    public void Text(string panel, string value)
    {
        if (_text.GetValueOrDefault(panel, string.Empty) == value) return;
        _entity.SetDialogVariableStringForPlayer(_playerId, panel, "value", value);
        _text[panel] = value;
    }

    public void Class(string panel, string name, bool enabled)
    {
        var key = (panel, name);
        if (_classes.GetValueOrDefault(key) == enabled) return;
        _entity.SetHasClassForPlayer(_playerId, panel, name, enabled
            ? EHudPanelClassStatus_t.k_eHudPanelClassStatus_HasClass
            : EHudPanelClassStatus_t.k_eHudPanelClassStatus_Undefined);
        _classes[key] = enabled;
    }

    public void Capture(bool enabled)
    {
        if (_capture == enabled) return;
        _entity.SetInputCaptureEnabledForPlayer(_playerId, enabled);
        _capture = enabled;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (!_entity.IsValidEntity) return;
        try { _entity.SetInputCaptureEnabledForPlayer(_playerId, false); }
        finally { if (_entity.IsValidEntity) _entity.Despawn(); }
    }
}
