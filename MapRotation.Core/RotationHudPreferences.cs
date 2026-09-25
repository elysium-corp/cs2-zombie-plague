using Common.Database.Sessions;
using Common.Database.Tasks;
using CustomHud.Api;
using MapRotation.Core.Database;
using SwiftlyS2.Shared.Players;

namespace MapRotation.Core;

/// <summary>Сессии редактируются игровым потоком; задачи хранения работают только с захваченными данными.</summary>
internal sealed class RotationHudPreferences(IRotationHudPreferenceStore store,
    SteamIdOperationQueue operations, DatabaseTaskTracker tasks) : IDisposable
{
    internal static HudMenuPresentation Default { get; } = new() { Orientation = HudMenuOrientation.Horizontal, ScalePercent = 80 };
    private HudMenuPresentation _default = Default;
    private readonly Dictionary<int, Entry> _players = [];
    private bool _disposed;

    public HudMenuPresentation Get(IPlayer player) => Ensure(player)?.Session.Read(x => x.Presentation) ?? _default;

    public void ConfigureDefaults(HudMenuPresentation presentation) => _default = presentation;

    public bool Set(IPlayer player, HudMenuPresentation presentation)
    {
        if (!Enum.IsDefined(presentation.Orientation) || presentation.ScalePercent is not (80 or 100 or 120)
            || !Enum.IsDefined(presentation.DockSide) || !Enum.IsDefined(presentation.Animation)
            || Ensure(player) is not { } entry) return false;
        entry.Session.Update(x => x.Presentation = presentation);
        Save(entry);
        return true;
    }

    private Entry? Ensure(IPlayer player)
    {
        if (_disposed || player is not { IsValid: true, IsFakeClient: false } || player.SteamID == 0 || player.SteamID > long.MaxValue) return null;
        if (_players.TryGetValue(player.PlayerID, out var current))
        {
            if (current.SessionId == player.SessionId && current.SteamId == player.SteamID) return current;
            Forget(player.PlayerID);
        }
        var defaults = _default;
        var entry = new Entry(player.SessionId, player.SteamID, defaults);
        _players[player.PlayerID] = entry;
        tasks.Run(token => operations.RunAsync(entry.SteamId, async () =>
        {
            try
            {
                var saved = await store.LoadAsync(entry.SteamId, token).ConfigureAwait(false);
                // CompleteLoad сохраняет ранний выбор игрока при медленном ответе БД.
                entry.Session.CompleteLoad(x => x.Presentation = saved ?? defaults);
            }
            catch
            {
                // Ошибка чтения не превращает стандартный вид в данные для записи.
                entry.Session.CompleteLoad(_ => { });
                throw;
            }
        }), "Загрузка настроек меню MapRotation");
        return entry;
    }

    private void Save(Entry entry) => tasks.Run(token => operations.RunAsync(entry.SteamId, async () =>
    {
        token.ThrowIfCancellationRequested();
        var snapshot = entry.Session.CreateSnapshot(x => x.Presentation);
        if (!snapshot.IsLoaded || !snapshot.IsDirty) return;
        await store.SaveAsync(entry.SteamId, snapshot.Data, token).ConfigureAwait(false);
        entry.Session.MarkSaved(snapshot.Revision);
    }), "Сохранение настроек меню MapRotation");

    public void Forget(int playerId)
    {
        if (_players.Remove(playerId, out var entry)) Save(entry);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var entry in _players.Values) Save(entry);
        tasks.StopAndWait();
        _players.Clear();
    }

    private sealed class Preference
    {
        public HudMenuPresentation Presentation { get; set; } = Default;
    }
    private sealed class Entry(ulong sessionId, ulong steamId, HudMenuPresentation defaults)
    {
        public ulong SessionId { get; } = sessionId;
        public ulong SteamId { get; } = steamId;
        public PersistentSession<Preference> Session { get; } = new(new() { Presentation = defaults });
    }
}
