using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Sounds;
using ZombiePlague.Core.Utils;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Zombies.Controller;

internal class ZombieSoundController : ISoundController
{
    private readonly IPlayer _playerOwner;
    private readonly List<string> _hurtSounds;
    private readonly ISwiftlyCore _core;
    
    private readonly Guid _onPlayerHurt;
    private readonly Guid _onPlayerDeath;

    private const int HurtSoundChanceInPercent = 50; 

    public ZombieSoundController(ISwiftlyCore core, Zombie zombieOwner)
    {
        _core = core;
        _playerOwner = zombieOwner.Player;
        _hurtSounds = zombieOwner.ZClass.HurtSounds;
        
        _onPlayerHurt = core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurt);
        _onPlayerDeath = core.GameEvent.HookPost<EventPlayerDeath>(OnPlayerDeath);
    }
    
    public void Dispose()
    {
        _core.GameEvent.Unhook(_onPlayerHurt);
        _core.GameEvent.Unhook(_onPlayerDeath);
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        var player = @event.UserIdPlayer;
        
        if (player != null && !player.Equals(_playerOwner))
        {
            return HookResult.Continue;
        }
        
        Dispose();
        
        return HookResult.Continue;
    }
    
    private HookResult OnPlayerHurt(EventPlayerHurt @event)
    {
        var player = @event.UserIdPlayer;
        
        if (player != null && !player.Equals(_playerOwner))
        {
            return HookResult.Continue;
        }
        
        if (CanEmitSound<EventPlayerHurt>())
        {
            EmitSound(GetRandomSound(_hurtSounds));
        }
        
        return HookResult.Continue;
    }

    private bool CanEmitSound<T>() where T : IGameEvent<T>
    {
        var playerPawn = _playerOwner.PlayerPawn;
        if (playerPawn == null || !playerPawn.IsValid)
        {
            return false;
        }
        
        if (typeof(T) == typeof(EventPlayerHurt))
        {
            return CanEmitSoundHurtEvent();
        }
        
        return true;
    }

    private bool CanEmitSoundHurtEvent()
    {
        return Numeric.Random(0, 100) < HurtSoundChanceInPercent;
    }

    private void EmitSound(string soundName)
    {
        if (soundName.IsNullOrEmpty())
        {
            return;
        }
        
        using var soundEvent = new SoundEvent
        {
            Volume = 1.0f,
            Name = soundName,
            SourceEntityIndex = (int)_playerOwner.PlayerPawn!.Index
        };
        
        soundEvent.Recipients.AddAllPlayers();
        soundEvent.Emit();
    }

    private string GetRandomSound(List<string> sounds)
    {
        return sounds.Count == 0 ? "" : sounds[Numeric.Random(0, sounds.Count)];
    }
}