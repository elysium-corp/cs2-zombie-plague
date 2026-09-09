using SwiftlyS2.Shared.Players;

namespace Shop.Core.Hud;

/// <summary>Скрывает перекрывающие магазин элементы HUD до завершения сессии.</summary>
internal sealed class ShopHudNativeVisibility : IDisposable
{
    // Флаги CBasePlayerPawn.m_iHideHUD: выбор оружия, прицел и радар.
    internal const uint HiddenElements = (1u << 0) | (1u << 8) | (1u << 12);
    private readonly Func<uint?> _read;
    private readonly Action<uint> _write;
    private readonly uint _addedBits;
    private bool _disposed;

    internal ShopHudNativeVisibility(Func<uint?> read, Action<uint> write)
    {
        _read = read;
        _write = write;
        if (read() is not { } flags) return;
        _addedBits = HiddenElements & ~flags;
        if (_addedBits != 0) write(flags | _addedBits);
    }

    public static ShopHudNativeVisibility Capture(IPlayer player)
    {
        // Копия handle сохраняет serial: после респавна/смены карты новый pawn
        // не получит восстановление флагов от предыдущей сущности или игрока.
        var handle = player.Controller.PlayerPawn;
        return new ShopHudNativeVisibility(
            () => handle.Value is { IsValidEntity: true } pawn ? pawn.HideHUD : null,
            flags =>
            {
                if (handle.Value is not { IsValidEntity: true } pawn) return;
                pawn.HideHUD = flags;
                pawn.HideHUDUpdated();
            });
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_addedBits == 0 || _read() is not { } flags) return;
        // Убираем только добавленные Shop биты, сохраняя остальные изменения HUD.
        var restored = flags & ~_addedBits;
        if (restored != flags) _write(restored);
    }
}
