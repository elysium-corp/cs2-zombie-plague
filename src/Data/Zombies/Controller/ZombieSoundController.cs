using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Di;
using CS2ZombiePlague.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Sounds;

namespace CS2ZombiePlague.Data.Zombies.Controller;

public class ZombieSoundController : ISoundController
{
    private readonly ISwiftlyCore _core = DependencyManager.GetService<ISwiftlyCore>();
    private readonly CommonUtils _commonUtils = DependencyManager.GetService<CommonUtils>();
    
    private readonly IPlayer _playerOwner;
    private readonly List<string> _hurtSounds;
    
    private readonly Guid _onPlayerHurt;
    private readonly Guid _onPlayerDeath;

    private const int HurtSoundChanceInPercent = 50; 

    public ZombieSoundController(Zombie zombieOwner)
    {
        _playerOwner = zombieOwner.GetPlayer();
        _hurtSounds = zombieOwner.GetZombieClass().HurtSounds;
        
        _onPlayerHurt = _core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurt);
        _onPlayerDeath = _core.GameEvent.HookPost<EventPlayerDeath>(OnPlayerDeath);
    }

    private HookResult OnPlayerDeath(EventPlayerDeath @event)
    {
        if (@event.UserIdPlayer != null && !@event.UserIdPlayer.Equals(_playerOwner))
        {
            return  HookResult.Continue;
        }

        var playerLifecycle = _playerOwner.GetLifecycle();
        playerLifecycle.SoundController = null;
        Dispose();
        
        return HookResult.Continue;
    }
    
    private HookResult OnPlayerHurt(EventPlayerHurt @event)
    {
        var player = @event.UserIdPlayer;
        if (player != null && !player.Equals(_playerOwner))
        {
            return  HookResult.Continue;
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
        return _commonUtils.RandomNum(0, 100) < HurtSoundChanceInPercent;
    }

    private void EmitSound(string soundName)
    {
        if (soundName.IsNullOrEmpty())
        {
            return;
        }
        
        using var soundEvent = new SoundEvent()
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
        return sounds.Count == 0 ? "" : sounds[_commonUtils.RandomNum(0, sounds.Count)];
    }
    
    public void Dispose()
    {
        _core.GameEvent.Unhook(_onPlayerHurt);
        _core.GameEvent.Unhook(_onPlayerDeath);
    }
}