using Common.Hooks;
using Common.Hooks.Abstractions;
using Microsoft.Extensions.Options;
using SupplyBox.Api.Events.Contexts;
using SupplyBox.Data.Configs;
using SupplyBox.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;
using ZombiePlague.Api;

namespace SupplyBox.Data.Entity;

public sealed class SupplyBoxEntity : ISupplyBoxEntity, IDisposable
{
    private readonly ISwiftlyCore _core;
    private readonly IZombiePlagueApi _api;
    private readonly IHookPublisher _hooks;
    private readonly string _parachuteSound;
    private readonly CBaseModelEntity? _entityParachute;

    private CancellationTokenSource? _pickUpThinker;
    private CancellationTokenSource? _dropThinker;
    private SupplyBoxEntityConfig? _data;
    private uint _soundGuid;
    private int _disposed;

    public CDynamicProp? Entity { get; }
    public int Index { get; private set; }

    public SupplyBoxEntity(
        ISwiftlyCore core,
        IHookPublisher hooks,
        IOptions<SupplyBoxConfig> config)
    {
        _core = core;
        _hooks = hooks;
        _api = SupplyBox.ZombiePlagueApi;

        var boxModel = config.Value.SupplyBoxModel;
        var parachuteModel = config.Value.ParachuteModel;
        _parachuteSound = config.Value.ParachuteSound;

        Entity = core.EntitySystem.CreateEntity<CDynamicProp>();
        _entityParachute = core.EntitySystem.CreateEntity<CBaseModelEntity>();

        core.Scheduler.NextWorldUpdate(() =>
        {
            if (Volatile.Read(ref _disposed) != 0) return;
            Entity.SetModel(boxModel);

            _entityParachute.SetModel(parachuteModel);
            _entityParachute.SetScale(0.9f);
            _entityParachute.AcceptInput<string>(
                "SetParent",
                "!activator",
                activator: Entity,
                caller: _entityParachute);
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
        Entity.Teleport(_data.Position + new Vector(0, 0, 1000), ToQAngles(_data.Rotation), null);

        if (_entityParachute != null)
        {
            _soundGuid = PlaySound(_parachuteSound);
            _entityParachute.DispatchSpawn();
            _entityParachute.Teleport(Entity.AbsOrigin + new Vector(-10, -5, -40), Entity.AbsRotation, null);
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
        if (Entity == null || !Entity.IsValidEntity || _data == null)
        {
            _dropThinker?.Cancel();
            return;
        }

        var entityPosition = Entity.AbsOrigin!.Value;

        if (entityPosition.Z > _data.Position.Z)
        {
            Entity.Teleport(Entity.AbsOrigin + new Vector(0, 0, -4), Entity.AbsRotation, null);
            return;
        }

        StopSound();
        _dropThinker?.Cancel();
        _entityParachute?.Despawn();

        var context = new SupplyBoxLandedContext(this);
        _hooks.Dispatch(ref context);
    }

    private void PickUpThinker()
    {
        if (Entity == null || !Entity.IsValidEntity)
        {
            _pickUpThinker?.Cancel();
            return;
        }

        var playersAround = MathAlgorithm.FindAllPlayersInSphere(50f, Entity.AbsOrigin!.Value);

        foreach (var player in playersAround)
        {
            if (CanPickUp(player) && TryPickUp(player))
            {
                return;
            }
        }
    }

    private bool CanPickUp(IPlayer player)
    {
        return player.IsValid &&
               player.IsAlive &&
               !_api.IsInfected(player);
    }

    private bool TryPickUp(IPlayer player)
    {
        var preContext = new SupplyBoxCollectingContext(player, this);

        if (!_hooks.DispatchCancellable(ref preContext))
        {
            DispatchCollectionRejected(player, SupplyBoxCollectionRejectionReason.Cancelled);
            return false;
        }

        if (!ReferenceEquals(preContext.SupplyBox, this))
        {
            DispatchCollectionRejected(preContext.Player, SupplyBoxCollectionRejectionReason.InvalidSupplyBox);
            return false;
        }

        if (!CanPickUp(preContext.Player))
        {
            DispatchCollectionRejected(preContext.Player, SupplyBoxCollectionRejectionReason.InvalidPlayer);
            return false;
        }

        if (!Destroy())
        {
            DispatchCollectionRejected(preContext.Player, SupplyBoxCollectionRejectionReason.DestructionCancelled);
            return false;
        }

        var postContext = new SupplyBoxCollectedContext(preContext.Player, this);
        _hooks.Dispatch(ref postContext);

        return true;
    }

    private bool Destroy()
    {
        var preContext = new SupplyBoxDestroyingContext(this);

        if (!_hooks.DispatchCancellable(ref preContext))
        {
            return false;
        }

        CleanupEntities();

        var postContext = new SupplyBoxDestroyedContext(this);
        _hooks.Dispatch(ref postContext);

        return true;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        CleanupEntities();
    }

    private void CleanupEntities()
    {
        _dropThinker?.Cancel();
        _dropThinker?.Dispose();
        _dropThinker = null;
        _pickUpThinker?.Cancel();
        _pickUpThinker?.Dispose();
        _pickUpThinker = null;
        StopSound();

        if (Entity is { IsValidEntity: true }) Entity.Despawn();
        if (_entityParachute is { IsValidEntity: true }) _entityParachute.Despawn();
    }

    private void DispatchCollectionRejected(
        IPlayer player,
        SupplyBoxCollectionRejectionReason reason
    )
    {
        var context = new SupplyBoxCollectionRejectedContext(player, this, reason);
        _hooks.Dispatch(ref context);
    }

    private static QAngle ToQAngles(Vector rotation)
    {
        return new QAngle(rotation.X, rotation.Y, rotation.Z);
    }

    private uint PlaySound(string soundName)
    {
        using var soundEvent = new SoundEvent
        {
            Volume = 1.7f,
            Name = soundName,
            SourceEntityIndex = (int)Entity!.Index
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
