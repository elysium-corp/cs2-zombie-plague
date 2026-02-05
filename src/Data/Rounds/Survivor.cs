using CS2ZombiePlague.Config;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Sounds;

namespace CS2ZombiePlague.Data.Rounds;

public class Survivor(ISwiftlyCore core, RoundManager roundManager, ZombieManager zombieManager, SurvivorConfig config) : IRound
{
    public void Start()
    {

        core.Event.OnWeaponServicesDropWeaponHook += OnWeaponServicesDropWeaponHook;
        
        var allPlayers = core.PlayerManager.GetAlive().ToList();
        var survivor = allPlayers[Random.Shared.Next(0, allPlayers.Count)];

        foreach (var player in allPlayers)
        {
            if (!player.Equals(survivor))
            {
                zombieManager.CreateZombie(player);
            }
        }
        Initialize(survivor);

        PlaySound();
        
        core.PlayerManager.SendCenter("Выживший => " + survivor.Controller.PlayerName);
    }
    
    public void End()
    {
        core.Event.OnWeaponServicesDropWeaponHook -= OnWeaponServicesDropWeaponHook;
        
        roundManager.SetRound(new None());

        core.PlayerManager.SendCenter("Раунд окончен");
    }

    public int GetChance()
    {
        return config.Chance;
    }

    private void Initialize(IPlayer survivor)
    {
        var countPlayers = core.PlayerManager.GetAlive().Count();
        var playerPawn = survivor.RequiredPlayerPawn;

        survivor.SetHealth(playerPawn.Health + (config.SurvivorBonusHealthPerZombie * countPlayers));
        survivor.SetModel(config.Model);

        var itemServices = playerPawn.ItemServices;
        if (itemServices == null) return;

        itemServices.RemoveItems();
        itemServices.GiveItem("weapon_knife_t");
        itemServices.GiveItem("weapon_negev");
    }
    
    private void PlaySound()
    {
        using var sound = new SoundEvent(config.MusicSoundName);
        
        sound.Recipients.AddAllPlayers();
        sound.SourceEntityIndex = -1;
        sound.Volume = 0.5f;

        sound.Emit();
    }

    private void OnWeaponServicesDropWeaponHook(IOnWeaponServicesDropWeaponHook @event)
    {
        var pawn = @event.WeaponServices.Pawn;

        if (pawn.Team == Team.CT && !@event.SwappingWeapon)
        {
            @event.Result = HookResult.Stop;
        }
    }
    
}