using CustomHud.Api;
using Common.Database.Storages;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Hud.AbilityHud;

internal sealed class AbilityHudSettings(PlayerSessionStore<PlayerPreferences> sessions)
{
    private HudWidgetOptions? _server;
    private AbilityHudPreferences ServerDefault => _server is null ? AbilityHudPreferences.Default
        : new AbilityHudPreferences(_server.ScalePercent, _server.Position).Normalize();
    internal bool AllowCustomization => _server?.AllowPlayerCustomization ?? true;
    internal void ApplyServerOptions(HudWidgetOptions options)
    {
        if (!AbilityHudPreferences.Scales.Contains(options.ScalePercent) || !AbilityHudPreferences.Positions.Contains(options.Position))
            throw new InvalidDataException("Некорректные настройки HUD способностей из CMS");
        _server = options;
    }

    public bool HasSession(ulong steamId) => sessions.Get(steamId) is not null;

    public AbilityHudPreferences Get(ulong steamId) => sessions.Get(steamId)?.Read(data => AllowCustomization && (data.AbilityHudCustomized || _server is null)
        ? data.AbilityHud.Normalize() : ServerDefault) ?? ServerDefault;

    public bool Update(ulong steamId, Func<AbilityHudPreferences, AbilityHudPreferences> update)
        => Update(steamId, update, reset: false);

    public bool Reset(ulong steamId) => Update(steamId, _ => ServerDefault, reset: true);

    private bool Update(ulong steamId, Func<AbilityHudPreferences, AbilityHudPreferences> update, bool reset)
    {
        if (!AllowCustomization) return false;
        var session = sessions.Get(steamId);
        if (session is null) return false;
        session.Update(data =>
        {
            var previous = data.AbilityHudCustomized || _server is null ? data.AbilityHud.Normalize() : ServerDefault;
            data.AbilityHud = update(previous).Normalize();
            data.AbilityHudCustomized = !reset;
            data.AbilityHudScaleChangedInSession |= reset || previous.ScalePercent != data.AbilityHud.ScalePercent;
            data.AbilityHudPositionChangedInSession |= reset || previous.Position != data.AbilityHud.Position;
        });
        return true;
    }
}
