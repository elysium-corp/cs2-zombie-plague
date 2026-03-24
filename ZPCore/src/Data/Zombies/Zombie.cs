using ZPCore.Data.Extensions;
using ZPCore.Data.Managers;
using ZPCore.Data.Zombies.Controller;
using ZPCore.Data.Zombies.ZClasses;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Zombies;

internal class Zombie : IZombie
{
    public IPlayer Player { get; }
    public IZClass ZClass { get; private set; }
    public bool IsNemesis { get; }
    
    private readonly ZombieManager _zombieManager;
    private ISoundController? _soundController;

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
        
        var savedZClass = _zombieManager.GetZClassFromMenu(Player.PlayerID);
        
        if (ZClass != savedZClass && !IsNemesis)
        {
            ZClass.Abilities.ForEach(ability => ability.UnHook());
            ZClass = savedZClass;
        }
        
        _soundController = new ZombieSoundController(this);
        
        var playerLifecycle = Player.GetLifecycle();
        playerLifecycle.SoundController =  _soundController;
        
        Player.SendAlert("Ваш класс => " + ZClass.DisplayName);

        Player.SetHealth(ZClass.Health);
        Player.SetSpeed(ZClass.Speed);
        Player.SetGravity(ZClass.Gravity);
        Player.SetModel(ZClass.Model);
        Player.SwitchTeam(Team.T);

        ZClass.Abilities.ForEach(zAbility => zAbility.SetCaster(Player));
        
        var itemServices = Player.PlayerPawn?.ItemServices;
        if (itemServices == null)
        {
            return;
        }

        itemServices.RemoveItems();
        itemServices.GiveItem("weapon_knife_t");
    }

    public void UnHookAbilities()
    {
        ZClass.Abilities.ForEach(zAbility => zAbility.UnHook());
    }
}