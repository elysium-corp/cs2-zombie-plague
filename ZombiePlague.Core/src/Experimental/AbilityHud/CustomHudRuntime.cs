using System.Reflection;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace ZombiePlague.Core.Experimental.AbilityHud;

internal sealed class CustomHudRuntime : IAbilityHudSink, IDisposable
{
    public const string Layout = "panorama/layout/custom_game/elysium_ability_buffs_v3.xml";
    public const string CompiledLayout = "panorama/layout/custom_game/elysium_ability_buffs_v3.vxml_c";
    public const string CompiledStyle = "panorama/styles/custom_game/elysium_ability_buffs_v3.vcss_c";
    private readonly CCSCustomHudLayout _entity;
    private readonly Action<CCSCustomHudLayout, int, string, string, string> _text;
    private readonly Action<CCSCustomHudLayout, int, string, string, EHudPanelClassStatus_t> _class;
    private bool _disposed;

    private CustomHudRuntime(ISwiftlyCore core,
        Action<CCSCustomHudLayout, int, string, string, string> text,
        Action<CCSCustomHudLayout, int, string, string, EHudPanelClassStatus_t> setClass,
        Action<CCSCustomHudLayout, bool> capture)
    {
        _text = text;
        _class = setClass;
        _entity = core.EntitySystem.CreateEntity<CCSCustomHudLayout>();
        try
        {
            _entity.StrLayout = Layout;
            _entity.StrLayoutUpdated();
            _entity.DispatchSpawn();
            capture(_entity, false);
        }
        catch { Dispose(); throw; }
    }

    public bool IsValid => !_disposed && _entity.IsValidEntity;

    // SDK 1.4.9 уже содержит API; проверка даёт понятную причину при устаревшем runtime сервера
    // Делегаты связываются один раз, вне обновления состояний игроков
    public static bool HasRequiredApi => Find("SetDialogVariableStringForPlayer", typeof(int), typeof(string), typeof(string), typeof(string)) is not null
        && Find("SetHasClassForPlayer", typeof(int), typeof(string), typeof(string), typeof(EHudPanelClassStatus_t)) is not null
        && Find("SetInputCaptureEnabled", typeof(bool)) is not null;

    private static MethodInfo? Find(string name, params Type[] arguments) => typeof(CCSCustomHudLayout).GetMethod(name, arguments);

    public static CustomHudRuntime Create(ISwiftlyCore core)
    {
        if (!HasRequiredApi)
            throw new NotSupportedException("В SwiftlyS2 отсутствует Custom HUD API — нужен runtime с SetHasClassForPlayer и SetDialogVariableStringForPlayer");
        var missing = MissingResources(path => core.GameFileSystem.FileExists(path, "GAME"));
        if (missing.Length > 0)
            throw new FileNotFoundException("HUD v3: в GAME отсутствуют " + string.Join(", ", missing)
                + ". Скомпилируйте ресурсы и смонтируйте обновлённый addon/VPK у сервера и клиента");
        return new(core,
            Find("SetDialogVariableStringForPlayer", typeof(int), typeof(string), typeof(string), typeof(string))!
                .CreateDelegate<Action<CCSCustomHudLayout, int, string, string, string>>(),
            Find("SetHasClassForPlayer", typeof(int), typeof(string), typeof(string), typeof(EHudPanelClassStatus_t))!
                .CreateDelegate<Action<CCSCustomHudLayout, int, string, string, EHudPanelClassStatus_t>>(),
            Find("SetInputCaptureEnabled", typeof(bool))!.CreateDelegate<Action<CCSCustomHudLayout, bool>>());
    }

    internal static string[] MissingResources(Func<string, bool> exists) => new[] { CompiledLayout, CompiledStyle }
        .Concat(AbilityHudFrame.Kinds.Select(kind => $"panorama/images/custom_game/elysium/abilities/{kind}.vsvg_c"))
        .Where(path => !exists(path)).ToArray();

    public void SetClass(int playerId, string panel, string name, bool enabled) => _class(_entity, playerId, panel, name,
        enabled ? EHudPanelClassStatus_t.k_eHudPanelClassStatus_HasClass : EHudPanelClassStatus_t.k_eHudPanelClassStatus_DoesNotHaveClass);

    public void SetText(int playerId, string panel, string value) => _text(_entity, playerId, panel, "value", value);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_entity.IsValidEntity) _entity.Despawn();
    }
}
