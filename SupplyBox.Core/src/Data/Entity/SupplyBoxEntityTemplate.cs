using Microsoft.Extensions.Options;
using SupplyBox.Data.Configs;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace SupplyBox.Data.Entity;

internal sealed class SupplyBoxEntityTemplate(ISwiftlyCore core, IOptions<SupplyBoxConfig> config) : ISupplyBoxEntity, IDisposable
{
    private IMenuAPI? _editMenu;
    private CancellationTokenSource? _thinker;
    private IPlayer? _player;
    private ulong _steamId;
    private int _disposed;
    public CDynamicProp? Entity { get; private set; }
    public int Index => 0;
    public Vector Rotation { get; set; }
    public Vector Position { get; set; }

    public void Spawn(IPlayer player)
    {
        if (_disposed != 0 || !player.IsValid || !player.IsAlive || player.PlayerPawn?.AbsOrigin is not { } position) return;
        _player = player; _steamId = player.SteamID; Position = position; Rotation = Vector.Zero;
        Entity = core.EntitySystem.CreateEntity<CDynamicProp>();
        if (Entity is null) return;
        Entity.SetModel(config.Value.SupplyBoxModel);
        Entity.Render = new Color(255, 255, 255, 100);
        Entity.DispatchSpawn();
        Entity.Teleport(position, new QAngle(0, 0, 0), null);
        _thinker = core.Scheduler.RepeatBySeconds(0.1f, Think);
    }

    public void SetMenu(IMenuAPI menu) => _editMenu = menu;
    public void Destroy() => Dispose();
    private void Think()
    {
        if (_disposed != 0 || Entity is not { IsValidEntity: true } || _player is not { IsValid: true, IsAlive: true }
            || _player.SteamID != _steamId || core.MenusAPI.GetCurrentMenu(_player) != _editMenu)
        { Dispose(); return; }
        Entity.Teleport(Position, new QAngle(Rotation.X, Rotation.Y, Rotation.Z), null);
    }
    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _thinker?.Cancel(); _thinker = null;
        if (Entity is { IsValidEntity: true }) Entity.Despawn();
    }
}
