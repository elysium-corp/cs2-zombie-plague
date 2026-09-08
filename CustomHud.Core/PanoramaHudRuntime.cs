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
    internal const string Layout = "panorama/layout/custom_game/elysium_messages_v4.vxml_c";
    internal const string Style = "panorama/styles/custom_game/elysium_messages_v4.vcss_c";
    private readonly CCSCustomHudLayout _entity;
    private bool _disposed;

    internal PanoramaHudRuntime(ISwiftlyCore core)
    {
        var missing = new[] { Layout, Style }.Where(path => !core.GameFileSystem.FileExists(path, "GAME")).ToArray();
        if (missing.Length > 0)
            throw new FileNotFoundException("Custom HUD: скомпилируйте и смонтируйте ресурсы VPK у сервера и клиентов: " + string.Join(", ", missing));
        _entity = core.EntitySystem.CreateEntity<CCSCustomHudLayout>();
        try
        {
            _entity.StrLayout = Layout;
            _entity.StrLayoutUpdated();
            _entity.DispatchSpawn();
            _entity.SetInputCaptureEnabled(false);
        }
        catch { Dispose(); throw; }
    }

    public bool IsValid => !_disposed && _entity.IsValidEntity;

    public void SetClass(int playerId, string panel, string name, bool enabled) =>
        _entity.SetHasClassForPlayer(playerId, panel, name, enabled
            ? EHudPanelClassStatus_t.k_eHudPanelClassStatus_HasClass
            : EHudPanelClassStatus_t.k_eHudPanelClassStatus_Undefined);

    public void SetText(int playerId, string panel, string text)
    {
        if (text.Length == 0) _entity.RemoveDialogVariableStringForPlayer(playerId, panel, "value");
        else _entity.SetDialogVariableStringForPlayer(playerId, panel, "value", text);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_entity.IsValidEntity) _entity.Despawn();
    }
}
