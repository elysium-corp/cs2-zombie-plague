using SwiftlyS2.Shared;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomHud.Core.Menus;

/// <summary>Персональный runtime меню на игровом потоке.</summary>
internal interface IHudMenuRuntime : IDisposable
{
    /// <summary>Сущность ещё действительна.</summary>
    bool IsValid { get; }
    /// <summary>Клик относится к текущей сущности открытия.</summary>
    bool Owns(CCSCustomHudLayout entity);
    /// <summary>Устанавливает строковую переменную value с подавлением повторной отправки.</summary>
    void Text(string panel, string value);
    /// <summary>Устанавливает класс только владельцу.</summary>
    void Class(string panel, string name, bool enabled);
    /// <summary>Показывает меню и устанавливает захват ввода.</summary>
    void Show(bool capture);
}

internal sealed class PanoramaMenuRuntime : IHudMenuRuntime
{
    internal const string Layout = "panorama/layout/custom_game/elysium_menu_v1.vxml_c";
    internal const string Style = "panorama/styles/custom_game/elysium_menu_v1.vcss_c";
    private readonly int _playerId;
    private readonly CCSCustomHudLayout _entity;
    private readonly Dictionary<string, string> _text = [];
    private readonly Dictionary<(string, string), bool> _classes = [];
    private bool _disposed;
    private bool? _capture;

    public PanoramaMenuRuntime(ISwiftlyCore core, int playerId)
    {
        foreach (var path in new[] { Layout, Style })
            if (!core.GameFileSystem.FileExists(path, "GAME")) throw new FileNotFoundException("Menu HUD: " + path);
        _playerId = playerId;
        _entity = core.EntitySystem.CreateEntity<CCSCustomHudLayout>();
        try
        {
            _entity.StrLayout = Layout;
            _entity.StrLayoutUpdated();
            _entity.DispatchSpawn();
            _entity.SetInputCaptureEnabled(false);
        }
        catch { if (_entity.IsValidEntity) _entity.Despawn(); throw; }
    }
    public bool IsValid => !_disposed && _entity.IsValidEntity;
    public bool Owns(CCSCustomHudLayout entity) => IsValid && entity.IsValidEntity && entity.Address == _entity.Address;
    public void Text(string panel, string value)
    {
        if (_text.TryGetValue(panel, out var previous) && previous == value) return;
        _entity.SetDialogVariableStringForPlayer(_playerId, panel, "value", value);
        _text[panel] = value;
    }
    public void Class(string panel, string name, bool enabled)
    {
        if (_classes.TryGetValue((panel, name), out var previous) && previous == enabled) return;
        _entity.SetHasClassForPlayer(_playerId, panel, name, enabled
            ? EHudPanelClassStatus_t.k_eHudPanelClassStatus_HasClass : EHudPanelClassStatus_t.k_eHudPanelClassStatus_DoesNotHaveClass);
        _classes[(panel, name)] = enabled;
    }
    public void Show(bool capture)
    {
        Class("MenuRoot", "Visible", true);
        if (_capture == capture) return;
        _entity.SetInputCaptureEnabledForPlayer(_playerId, capture);
        _capture = capture;
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
