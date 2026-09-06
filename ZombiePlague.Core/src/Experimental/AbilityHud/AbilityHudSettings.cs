using Common.Database.Storages;
using SwiftlyS2.Shared.Menus;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Experimental.AbilityHud;

internal sealed class AbilityHudSettings(PlayerSessionStore<PlayerPreferences> sessions)
{
    internal static readonly object PreviewMenuTag = new();

    public bool HasSession(ulong steamId) => sessions.Get(steamId) is not null;

    public AbilityHudPreferences Get(ulong steamId) => sessions.Get(steamId)?.Read(data => data.AbilityHud.Normalize())
        ?? AbilityHudPreferences.Default;

    public bool Update(ulong steamId, Func<AbilityHudPreferences, AbilityHudPreferences> update)
        => Update(steamId, update, reset: false);

    public bool Reset(ulong steamId) => Update(steamId, _ => AbilityHudPreferences.Default, reset: true);

    private bool Update(ulong steamId, Func<AbilityHudPreferences, AbilityHudPreferences> update, bool reset)
    {
        var session = sessions.Get(steamId);
        if (session is null) return false;
        session.Update(data =>
        {
            var previous = data.AbilityHud.Normalize();
            data.AbilityHud = update(previous).Normalize();
            data.AbilityHudScaleChangedInSession |= reset || previous.ScalePercent != data.AbilityHud.ScalePercent;
            data.AbilityHudPositionChangedInSession |= reset || previous.Position != data.AbilityHud.Position;
        });
        return true;
    }

    public static bool ShouldHideForMenu(IMenuAPI? menu, bool hideWhenMenuOpen) => hideWhenMenuOpen
        && menu is not null && !ReferenceEquals(menu.Tag, PreviewMenuTag);
}
