using ZPCore.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace ZPCore.Data.Plugins.SupplyBox;

internal sealed class SupplyBoxEntityTemplate
{
    private readonly ISwiftlyCore _core = DependencyManager.GetService<ISwiftlyCore>();
    private readonly IPlayer _player;
    private IMenuAPI? _editMenu;
    private CancellationTokenSource? _thinker;
    private const string BoxModel = "models/props/crates/cs2_drop_crate_01.vmdl";
        
    public CDynamicProp? Entity { get; }
    public Vector Rotation { get; set; }
    public Vector Position { get; set; }
    
    public SupplyBoxEntityTemplate(IPlayer player)
    {
        _player =  player;
        
        Entity = _core.EntitySystem.CreateEntity<CDynamicProp>();
        
        _core.Scheduler.NextWorldUpdate(()=>
        {
            Entity.SetModel(BoxModel);
            Entity.Render = new Color(255, 255, 255, 100);
        });
    }

    public void Spawn()
    {
        if (Entity == null || !_player.IsValid)
        {
            return;
        }

        var position = _player.PlayerPawn!.AbsOrigin!.Value;
        Position = position;
        Rotation = Vector.Zero;
        
        Entity.DispatchSpawn();
        Entity.Teleport(position, ToQAngles(Rotation), null);
        
        SetThinker();
    }
    
    public void Destroy()
    {
        if (Entity != null && Entity.IsValidEntity)
        {
            Entity.Despawn();
        }
        
        _thinker?.Cancel();
    }

    public void SetMenu(IMenuAPI menu)
    {
        _editMenu = menu;
    }
    
    private void SetThinker()
    {
        _thinker = _core.Scheduler.RepeatBySeconds(0.1f, Thinker);
    }

    private void Thinker()
    {
        if (Entity == null || !Entity.IsValidEntity || !_player.IsValid)
        {
            Destroy();
            return;
        }

        if (Entity!.AbsRotation!.Value != ToQAngles(Rotation))
        {
            Entity.Teleport(Entity.AbsOrigin, ToQAngles(Rotation), null);
        }
        

        var currentMenu = _core.MenusAPI.GetCurrentMenu(_player);

        if (currentMenu == null)
        {
            Destroy();
            return;
        }
        
        if (currentMenu != _editMenu)
        {
            Destroy();
        }
    }

    private QAngle ToQAngles(Vector rotation)
    {
        return new QAngle(rotation.X, rotation.Y, rotation.Z);
    }
}