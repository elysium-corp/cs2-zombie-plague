using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Zombies.Controller;
using ZombiePlague.Core.Data.Zombies.ZClasses;
using ZombiePlague.Core.Utils.Extensions;
using ZPCore.Data.Zombies.Controller;

namespace ZombiePlague.Core.Data.Zombies;

internal class Zombie : IZombie
{
    public IPlayer Player { get; }
    public IZClass ZClass { get; private set; }
    public bool IsNemesis { get; }
    public ISoundController? SoundController {get; private set;}
    
    private readonly ZombieManager _zombieManager;

    public Zombie(ISwiftlyCore core, ZombieManager zombieManager, IPlayer player, IZClass zClass,
        bool isNemesis = false)
    {
        Player = player;
        ZClass = zClass;
        IsNemesis = isNemesis;
        _zombieManager = zombieManager;
        
        core.Scheduler.NextWorldUpdate(Initialize);
    }

    public void Initialize()
    {
        if (!Player.IsAlive)
        {
            return;
        }
        
        var savedZClass = _zombieManager.GetZClassFromMenu(Player);
        
        if (ZClass != savedZClass && !IsNemesis)
        {
            ZClass.Abilities.ForEach(ability => ability.UnHook());
            ZClass = savedZClass;
        }
        TryChangeZClass();
        SetProperties();
        
        ZClass.Abilities.ForEach(zAbility => zAbility.SetCaster(Player));
        SoundController = new ZombieSoundController(this);
        
        Player.SendAlert("Ваш класс => " + ZClass.DisplayName);
    }

    public void UnHookAbilities()
    {
        ZClass.Abilities.ForEach(zAbility => zAbility.UnHook());
    }

    private void SetProperties()
    {
        Player.SetHealth(ZClass.Health);
        Player.SetSpeed(ZClass.Speed);
        Player.SetGravity(ZClass.Gravity);
        Player.SetModel(ZClass.Model);
        Player.SwitchTeam(Team.T);
        
        var itemServices = Player.PlayerPawn?.ItemServices;
        if (itemServices == null)
        {
            return;
        }

        itemServices.RemoveItems();
        itemServices.GiveItem("weapon_knife_t");
    }

    private bool TryChangeZClass()
    {
        var savedZClass = _zombieManager.GetZClassFromMenu(Player);

        if (ZClass == savedZClass || IsNemesis)
        {
            return false;
        }
        
        ZClass.Abilities.ForEach(ability => ability.UnHook());
        ZClass = savedZClass;

        return true;
    }
}