using ZPCore.Data.Extensions;
using ZPCore.Di;
using ZPCore.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;

namespace ZPCore.Data.Plugins.SupplyBox;

internal sealed class SupplyBoxEntity
{
    private readonly ISwiftlyCore _core = DependencyManager.GetService<ISwiftlyCore>();
    // private readonly IEventPublisher _eventPublisher = DependencyManager.GetService<IEventPublisher>();
    
    private const string BoxModel = "models/props/crates/cs2_drop_crate_01.vmdl";
    private const string ParachuteModel = "characters/nozb1/parachute/parachute_carbon/parachute_open.vmdl";
    private const string FallSound = "ZombiePlagueSupplyBox.SupplyboxFly";
    
    private readonly SupplyBoxEntityConfig _data;
    private CBaseModelEntity? _entityParachute;
    private CancellationTokenSource? _pickUpThinker;
    private CancellationTokenSource? _dropThinker;
    private uint _soundGuid;
    
    public CDynamicProp? Entity { get; }
    public int Index { get; }

    public SupplyBoxEntity(SupplyBoxEntityConfig config)
    {
        Index = config.Index;
        
        _data = config;
        
        Entity = _core.EntitySystem.CreateEntity<CDynamicProp>();
        
        _entityParachute = _core.EntitySystem.CreateEntity<CBaseModelEntity>();
        
        _core.Scheduler.NextWorldUpdate(()=>
        {
            Entity.SetModel(BoxModel);
            
            _entityParachute.SetModel(ParachuteModel);
            _entityParachute.SetScale(0.9f);
            _entityParachute.AcceptInput<string>("SetParent", "!activator", activator: Entity, caller: _entityParachute);
        });
    }

    public void Spawn()
    {
        if (Entity == null)
        {
            return;
        }
        
        Entity.DispatchSpawn();
        Entity.Teleport(_data.Position + new Vector(0,0,1000), ToQAngles(_data.Rotation), null);
        
        if (_entityParachute != null)
        {
            _soundGuid = PlaySound(FallSound);
            _entityParachute.DispatchSpawn();
            _entityParachute.Teleport(Entity.AbsOrigin + new Vector(-10,-5,-40), Entity.AbsRotation, null);
        }
        
        SetThinkers();
    }

    private void SetThinkers()
    {
        _dropThinker = _core.Scheduler.RepeatBySeconds(0.03f, DropThinker);
        _pickUpThinker = _core.Scheduler.RepeatBySeconds(0.05f, PickUpThinker);
    }

    private void DropThinker()
    {
        if (Entity == null || !Entity.IsValidEntity)
        {
            _dropThinker?.Cancel();
            return;
        }

        if (_data == null)
        {
            _dropThinker?.Cancel();
            return;
        }
        
        var entityPosition = Entity!.AbsOrigin!.Value;
        
        if (entityPosition.Z > _data.Position.Z)
        {
            Entity.Teleport(Entity.AbsOrigin + new Vector(0,0,-5), Entity.AbsRotation, null);
        }
        else
        {
            StopSound();
            _dropThinker?.Cancel();
            _entityParachute?.Despawn();
        }
    }
    
    private void PickUpThinker()
    {
        if (Entity == null || !Entity.IsValidEntity)
        {
            _pickUpThinker?.Cancel();
            return;
        }
        
        var playerAround = MathAlgorithm.FindAllPlayersInSphere(50f, Entity!.AbsOrigin!.Value);
        foreach (var player in playerAround)
        {
            if (CanPickUp(player))
            {
                _pickUpThinker?.Cancel();
                
                PickUp(player);
                
                return;
            }
        }
    }

    private bool CanPickUp(IPlayer player)
    {
        if (player.IsInfected())
        {
            return false;
        }

        return true;
    }

    private void PickUp(IPlayer player)
    {
        // _eventPublisher.OnSupplyBoxPickedUp(player, this);
        
        Destroy();
    }
    
    private void Destroy()
    {
        if (Entity != null && Entity.IsValidEntity)
        {
            Entity.Despawn();
        }
        
        if (_entityParachute != null && _entityParachute.IsValidEntity)
        {
            _entityParachute.Despawn();
        }
        
        _dropThinker?.Cancel();
        _pickUpThinker?.Cancel();
    }
    
    private QAngle ToQAngles(Vector rotation)
    {
        return new QAngle(rotation.X, rotation.Y, rotation.Z);
    }

    private uint PlaySound(string soundName)
    {
        using var soundEvent = new SoundEvent()
        {
            Volume = 1.7f,
            Name = soundName,
            SourceEntityIndex = (int)Entity.Index
        };
        
        soundEvent.Recipients.AddAllPlayers();
        return soundEvent.Emit();
    }
    
    private void StopSound()
    {
        if (_soundGuid == 0) return;

        using var stop = _core.NetMessage.Create<CMsgSosStopSoundEvent>();
        stop.SoundeventGuid = unchecked((int)_soundGuid);
        stop.SendToAllPlayers();

        _soundGuid = 0;
    }
}