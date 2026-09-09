using Common.Database.Sessions;
using Common.Database.Tasks;
using Shop.Core.Database;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Hud;

internal sealed class ShopHudPreference
{
    internal static readonly int[] Scales = [75, 85, 100, 115, 125];
    public int ScalePercent { get; set; }
    // Ноль означает выбор серверного масштаба по умолчанию, без личного переопределения.
    internal static int Normalize(int value) => Scales.Contains(value) ? value : 100;
}

internal enum ShopHudSaveStatus { Ready, Loading, Saving, Failed }
internal readonly record struct ShopHudSettingsView(int ScalePercent, bool CanEdit, ShopHudSaveStatus Status);

// Словарь подключений используется только игровым потоком. Фоновые задачи работают
// с захваченной сессией, не обращаются к игроку и не меняют состояние нового подключения.
internal sealed class ShopHudPreferences(
    IShopHudPreferenceStore store,
    SteamIdOperationQueue operations,
    DatabaseTaskTracker tasks) : IDisposable
{
    private readonly Dictionary<int, Entry> _players = [];
    private bool _disposed;

    public ShopHudSettingsView Get(IPlayer player, int defaultScale = 100)
    {
        var entry = Ensure(player);
        if (entry is null) return new(defaultScale, false, ShopHudSaveStatus.Ready);
        var snapshot = entry.Session.CreateSnapshot(x => x.ScalePercent);
        var status = entry.Failed ? ShopHudSaveStatus.Failed : !snapshot.IsLoaded
            ? ShopHudSaveStatus.Loading : snapshot.IsDirty ? ShopHudSaveStatus.Saving : ShopHudSaveStatus.Ready;
        return new(snapshot.Data == 0 ? defaultScale : snapshot.Data, true, status);
    }

    public bool Set(IPlayer player, int scalePercent)
    {
        if (!ShopHudPreference.Scales.Contains(scalePercent) || Ensure(player) is not { } entry) return false;
        entry.Session.Update(x => x.ScalePercent = scalePercent);
        entry.Failed = false;
        Save(entry);
        return true;
    }

    private Entry? Ensure(IPlayer player)
    {
        if (_disposed || player is not { IsValid: true, IsAuthorized: true, IsFakeClient: false }
            || player.SteamID == 0 || player.SteamID > long.MaxValue) return null;
        if (_players.TryGetValue(player.PlayerID, out var existing))
        {
            if (existing.SessionId == player.SessionId && existing.SteamId == player.SteamID) return existing;
            Forget(player.PlayerID);
        }
        var entry = new Entry(player.SessionId, player.SteamID);
        _players[player.PlayerID] = entry;
        tasks.Run(token => operations.RunAsync(entry.SteamId, async () =>
        {
            try
            {
                var scale = await store.LoadAsync(entry.SteamId, token).ConfigureAwait(false);
                entry.Session.CompleteLoad(x => x.ScalePercent = scale is null ? 0 : ShopHudPreference.Normalize(scale.Value));
            }
            catch
            {
                entry.Failed = true;
                // После ошибки чтения не записываем default. Явный выбор игрока
                // всё равно можно сохранить: таблица содержит только масштаб.
                entry.Session.CompleteLoad(_ => { });
                throw;
            }
        }), "Загрузка масштаба магазина");
        return entry;
    }

    private void Save(Entry entry)
    {
        tasks.Run(token => operations.RunAsync(entry.SteamId, async () =>
        {
            token.ThrowIfCancellationRequested();
            var snapshot = entry.Session.CreateSnapshot(x => x.ScalePercent);
            if (!snapshot.IsLoaded || !snapshot.IsDirty) return;
            try
            {
                await store.SaveAsync(entry.SteamId, snapshot.Data, token).ConfigureAwait(false);
                entry.Session.MarkSaved(snapshot.Revision);
                entry.Failed = false;
            }
            catch
            {
                entry.Failed = true;
                throw;
            }
        }), "Сохранение масштаба магазина");
    }

    public void Forget(int playerId)
    {
        if (_players.Remove(playerId, out var entry)) Save(entry);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Каждый выбор уже поставлен в очередь. Останавливаем её до удаления DI и БД.
        tasks.StopAndWait();
        _players.Clear();
    }

    private sealed class Entry(ulong sessionId, ulong steamId)
    {
        public ulong SessionId { get; } = sessionId;
        public ulong SteamId { get; } = steamId;
        public PersistentSession<ShopHudPreference> Session { get; } = new(new());
        public volatile bool Failed;
    }
}
