using Microsoft.Extensions.Options;
using SupplyBox.Data.Configs;
using SupplyBox.Events;
using SupplyBox.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;
using ZombiePlague.Api;

namespace SupplyBox.Data.Entity;

public sealed class SupplyBoxEntity : ISupplyBoxEntity
{
    private readonly ISwiftlyCore _core;
    private readonly IZombiePlagueApi _api;
    private readonly IEventPublisher _eventPublisher;
    
    private readonly string _parachuteSound;
    
    private readonly CBaseModelEntity? _entityParachute;
    private CancellationTokenSource? _pickUpThinker;
    private CancellationTokenSource? _dropThinker;
    
    private SupplyBoxEntityConfig? _data;
    private uint _soundGuid;
    
    public CDynamicProp? Entity { get; }
    public int Index { get; private set; }

    public SupplyBoxEntity(ISwiftlyCore core, IEventPublisher eventPublisher, IOptions<SupplyBoxConfig> config)
    {
        _core = core;
        _eventPublisher = eventPublisher;
        _api = SupplyBox.ZombiePlagueApi;
        
        var boxModel = config.Value.SupplyBoxModel;
        var parachuteModel = config.Value.ParachuteModel;
        _parachuteSound = config.Value.ParachuteSound;
        
        Entity = core.EntitySystem.CreateEntity<CDynamicProp>();
        
        _entityParachute = core.EntitySystem.CreateEntity<CBaseModelEntity>();
        
        core.Scheduler.NextWorldUpdate(()=>
        {
            Entity.SetModel(boxModel);
            
            _entityParachute.SetModel(parachuteModel);
            _entityParachute.SetScale(0.9f);
            _entityParachute.AcceptInput<string>("SetParent", "!activator", activator: Entity, caller: _entityParachute);
        });
    }
    
    public void Spawn(SupplyBoxEntityConfig config)
    {
        if (Entity == null)
        {
            return;
        }
        
        _data = config;
        Index = config.Index;
        
        Entity.DispatchSpawn();
        Entity.Teleport(_data.Position + new Vector(0,0,1000), ToQAngles(_data.Rotation), null);
        
        if (_entityParachute != null)
        {
            _soundGuid = PlaySound(_parachuteSound);
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
        if (_api.IsInfected(player))
        {
            return false;
        }

        return true;
    }

    private void PickUp(IPlayer player)
    {
        _eventPublisher.OnSupplyBoxPickedUp(player, this);
        
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