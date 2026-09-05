using Common.Hooks;
using Common.Hooks.Abstractions;
using Microsoft.Extensions.Options;
using SupplyBox.Api.Events.Contexts;
using SupplyBox.Configuration;
using SupplyBox.Data.Configs;
using SupplyBox.Services;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Sounds;

namespace SupplyBox.Data.Entity;

internal sealed class SupplyBoxEntity(ISwiftlyCore core, IHookPublisher hooks,
    IOptions<SupplyBoxConfig> options, SupplyBoxRewardService rewards) : ISupplyBoxEntity, IDisposable
{
    private readonly SupplyBoxConfig _settings = options.Value;
    private CDynamicProp? _parachute;
    private CancellationTokenSource? _thinker;
    private SupplyBoxType? _type;
    private Vector _target;
    private long _lastTick;
    private long _landedAt;
    private long _nextPickup;
    private uint _sound;
    private bool _collecting;
    private int _disposed;
    public CDynamicProp? Entity { get; private set; }
    public int Index { get; private set; }
    public bool IsAlive => _disposed == 0 && Entity is { IsValidEntity: true };

    public bool Spawn(SupplyBoxPoint point, SupplyBoxType type)
    {
        _type = type;
        Index = point.Id;
        _target = new((float)point.X, (float)point.Y, (float)point.Z);
        var angles = new QAngle((float)point.Pitch, (float)point.Yaw, (float)point.Roll);
        Entity = core.EntitySystem.CreateEntity<CDynamicProp>();
        if (Entity is null) return false;
        Entity.SetModel(type.Model);
        Entity.DispatchSpawn();
        Entity.Teleport(_target + new Vector(0, 0, _settings.DropHeight), angles, null);
        var parachuteModel = type.ParachuteModel.Length > 0 ? type.ParachuteModel : _settings.ParachuteModel;
        if (parachuteModel.Length > 0 && _settings.DropHeight > 0)
        {
            _parachute = core.EntitySystem.CreateEntity<CDynamicProp>();
            if (_parachute is not null)
            {
                _parachute.SetModel(parachuteModel);
                _parachute.DispatchSpawn();
                _parachute.Teleport(Entity.AbsOrigin + new Vector(0, 0, 30), angles, null);
                _parachute.AcceptInput<string>("SetParent", "!activator", activator: Entity, caller: _parachute);
            }
        }
        var sound = type.FallingSound.Length > 0 ? type.FallingSound : _settings.ParachuteSound;
        if (sound.Length > 0 && _settings.DropHeight > 0)
        {
            using var soundEvent = new SoundEvent { Name = sound, Volume = 1.0f, SourceEntityIndex = (int)Entity.Index };
            soundEvent.Recipients.AddAllPlayers(); _sound = soundEvent.Emit();
        }
        _lastTick = Environment.TickCount64;
        _thinker = core.Scheduler.RepeatBySeconds(0.05f, Think);
        return true;
    }

    private void Think()
    {
        if (!IsAlive || Entity?.AbsOrigin is not { } position) { Dispose(); return; }
        var now = Environment.TickCount64;
        if (_landedAt == 0)
        {
            var delta = Math.Clamp((now - _lastTick) / 1000f, 0, 0.25f);
            _lastTick = now;
            var z = Math.Max(_target.Z, position.Z - _settings.FallSpeed * delta);
            Entity.Teleport(new Vector(_target.X, _target.Y, z), null, null);
            if (z > _target.Z) return;
            _landedAt = now;
            StopSound();
            if (_parachute is { IsValidEntity: true }) _parachute.Despawn();
            _parachute = null;
            var landed = new SupplyBoxLandedContext(this); hooks.Dispatch(ref landed);
            if (!IsAlive) return;
        }
        if (_settings.LifetimeSeconds > 0 && now - _landedAt >= _settings.LifetimeSeconds * 1000L) { Dispose(); return; }
        if (now < _nextPickup) return;
        _nextPickup = now + 200;
        foreach (var player in core.PlayerManager.GetAlive())
        {
            if (!CanCollect(player) || player.PlayerPawn?.AbsOrigin is not { } origin) continue;
            var delta = origin - _target;
            if (delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z > _settings.PickupRadius * _settings.PickupRadius) continue;
            if (TryCollect(player)) return;
        }
    }

    private bool CanCollect(IPlayer player) => player.IsValid && player.IsAlive
        && player.PlayerPawn is { IsValid: true }
        && (SupplyBox.ZombiePlagueApi.IsInfected(player) ? _settings.ZombiesCanCollect : _settings.HumansCanCollect)
        && rewards.CanCollect(player, _settings);

    private bool TryCollect(IPlayer player)
    {
        if (_collecting || !IsAlive) return false;
        _collecting = true;
        try
        {
            var collecting = new SupplyBoxCollectingContext(player, this);
            if (!hooks.DispatchCancellable(ref collecting)) return Reject(player, SupplyBoxCollectionRejectionReason.Cancelled);
            if (!ReferenceEquals(collecting.SupplyBox, this)) return Reject(player, SupplyBoxCollectionRejectionReason.InvalidSupplyBox);
            player = collecting.Player;
            if (!IsAlive || !CanCollect(player)) return Reject(player, SupplyBoxCollectionRejectionReason.InvalidPlayer);
            var destroying = new SupplyBoxDestroyingContext(this);
            if (!hooks.DispatchCancellable(ref destroying)) return Reject(player, SupplyBoxCollectionRejectionReason.DestructionCancelled);
            // Ящик остаётся на карте, если ни одну доступную награду выдать не удалось.
            if (!IsAlive || !rewards.TryGrant(player, _type!, _settings)) return Reject(player, SupplyBoxCollectionRejectionReason.RewardUnavailable);
            Dispose();
            var collected = new SupplyBoxCollectedContext(player, this); hooks.Dispatch(ref collected);
            return true;
        }
        finally { _collecting = false; }
    }

    private bool Reject(IPlayer player, SupplyBoxCollectionRejectionReason reason)
    {
        var rejected = new SupplyBoxCollectionRejectedContext(player, this, reason); hooks.Dispatch(ref rejected);
        return false;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _thinker?.Cancel(); _thinker = null;
        StopSound();
        if (Entity is { IsValidEntity: true }) Entity.Despawn();
        if (_parachute is { IsValidEntity: true }) _parachute.Despawn();
        var destroyed = new SupplyBoxDestroyedContext(this); hooks.Dispatch(ref destroyed);
    }

    private void StopSound()
    {
        if (_sound == 0) return;
        using var stop = core.NetMessage.Create<CMsgSosStopSoundEvent>();
        stop.SoundeventGuid = unchecked((int)_sound); stop.SendToAllPlayers(); _sound = 0;
    }
}
