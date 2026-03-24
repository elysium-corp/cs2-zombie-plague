using ZPCore.Data.Extensions;
using ZPCore.Data.Managers;
using ZPCore.Data.Zombies.Controller;
using ZPCore.Data.Zombies.ZClasses;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Zombies;

internal class Zombie
{
    private readonly ZombieManager _zombieManager;
    private readonly IPlayer _player;
    
    private ISoundController? _soundController;
    private IZClass _zClass;
    public bool IsNemesis { get; }

    public Zombie(ISwiftlyCore core, ZombieManager zombieManager, IPlayer player, IZClass zClass,
        bool isNemesis = false)
    {
        _zombieManager = zombieManager;
        _player = player;
        _zClass = zClass;
        IsNemesis = isNemesis;

        core.Scheduler.NextWorldUpdate(Initialize);
    }

    public void Initialize()
    {
        if (!_player.IsAlive)
        {
            return;
        }
        
        if (_zClass != _zombieManager.GetZClassFromMenu(_player.PlayerID) && !IsNemesis)
        {
            _zClass.Abilities.ForEach(ability => ability.UnHook());
            _zClass = _zombieManager.GetZClassFromMenu(_player.PlayerID);
        }
        
        _soundController = new ZombieSoundController(this);
        var playerLifecycle = _player.GetLifecycle();
        playerLifecycle.SoundController =  _soundController;
        
        _player.SendAlert("Ваш класс => " + _zClass.DisplayName);

        _player.SetHealth(_zClass.Health);
        _player.SetSpeed(_zClass.Speed);
        _player.SetGravity(_zClass.Gravity);
        _player.SetModel(_zClass.Model);
        _player.SwitchTeam(Team.T);

        _zClass.Abilities.ForEach(zAbility => zAbility.SetCaster(_player));
        
        var itemServices = _player.PlayerPawn?.ItemServices;
        if (itemServices == null)
        {
            return;
        }

        itemServices.RemoveItems();
        itemServices.GiveItem("weapon_knife_t");
    }

    public void UnHookAbilities()
    {
        _zClass.Abilities.ForEach(zAbility => zAbility.UnHook());
    }

    public IPlayer GetPlayer()
    {
        return _player;
    }

    public IZClass GetZombieClass()
    {
        return _zClass;
    }
}