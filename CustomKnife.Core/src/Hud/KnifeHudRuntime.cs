using SwiftlyS2.Shared;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomKnife.Hud;

/// <summary>Персональный HUD; все операции выполняются на игровом потоке.</summary>
internal interface IKnifeHudRuntime : IDisposable
{
    /// <summary>Существует ли сущность на текущей карте.</summary>
    bool IsValid { get; }
    /// <summary>Принадлежит ли событие клика этой сущности.</summary>
    bool Owns(CCSCustomHudLayout entity);
    /// <summary>Обновляет строковую переменную value панели.</summary>
    void Text(string panel, string value);
    /// <summary>Устанавливает класс только владельцу HUD.</summary>
    void Class(string panel, string name, bool enabled);
    /// <summary>Заменяет предыдущий класс в одной группе оформления.</summary>
    void Choice(string panel, string group, string value);
    /// <summary>Проверяет регистрацию имени в состоянии HUD; не подтверждает наличие класса в CSS.</summary>
    bool IsClassRegistered(string name);
    /// <summary>Показывает меню и захватывает мышь владельца.</summary>
    void Show();
}

internal sealed class KnifeHudRuntime : IKnifeHudRuntime
{
    internal const string Layout = "panorama/layout/custom_game/elysium_knife_selector_v4.vxml_c";
    internal const string Style = "panorama/styles/custom_game/elysium_knife_selector_v4.vcss_c";
    internal const string ImagesStyle = "panorama/styles/custom_game/elysium_knife_images_v4.vcss_c";
    internal const string CmsStyle = "panorama/styles/custom_game/elysium_knife_cms_v4.vcss_c";
    private readonly CCSCustomHudLayout _entity;
    private readonly int _playerId;
    private readonly Dictionary<string, string> _text = [];
    private readonly Dictionary<(string, string), bool> _classes = [];
    private readonly Dictionary<(string, string), string> _choices = [];
    private bool _disposed;
    private bool _shown;

    public KnifeHudRuntime(ISwiftlyCore core, int playerId)
    {
        foreach (var path in new[] { Layout, Style, ImagesStyle, CmsStyle })
            if (!core.GameFileSystem.FileExists(path, "GAME"))
                throw new FileNotFoundException("Knife HUD: отсутствует ресурс " + path);
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

    public bool IsClassRegistered(string name)
    {
        if (!IsValid) return false;
        for (var index = 0; index < _entity.ClassNames.Count; index++)
            if (_entity.ClassNames[index] == name) return true;
        return false;
    }

    public void Text(string panel, string value)
    {
        if (_text.TryGetValue(panel, out var previous) && previous == value) return;
        _entity.SetDialogVariableStringForPlayer(_playerId, panel, "value", value);
        _text[panel] = value;
    }

    public void Class(string panel, string name, bool enabled)
    {
        var key = (panel, name);
        if (_classes.TryGetValue(key, out var previous) && previous == enabled) return;
        _entity.SetHasClassForPlayer(_playerId, panel, name, enabled
            ? EHudPanelClassStatus_t.k_eHudPanelClassStatus_HasClass
            : EHudPanelClassStatus_t.k_eHudPanelClassStatus_DoesNotHaveClass);
        _classes[key] = enabled;
    }

    public void Choice(string panel, string group, string value)
    {
        var key = (panel, group);
        if (_choices.TryGetValue(key, out var previous))
        {
            if (previous == value) return;
            Class(panel, previous, false);
        }
        Class(panel, value, true);
        _choices[key] = value;
    }

    public void Show()
    {
        if (_shown) return;
        Class("KnifeRoot", "Visible", true);
        _entity.SetInputCaptureEnabledForPlayer(_playerId, true);
        _shown = true;
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
