using CS2ZombiePlague.Data.Abilities.Contracts;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Data.Zombies.ZClasses;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Zombies;

public class Zombie
{
    private readonly ZombieManager _zombieManager;
    private readonly IPlayer _player;
    private IZClass _iZClass;
    public bool IsNemesis { get; }

    public Zombie(ISwiftlyCore core, ZombieManager zombieManager, IPlayer player, IZClass izClass,
        bool isNemesis = false)
    {
        _zombieManager = zombieManager;
        _player = player;
        _iZClass = izClass;
        IsNemesis = isNemesis;

        core.Scheduler.NextWorldUpdate(Initialize);
    }

    public void Initialize()
    {
        if (!_player.IsAlive)
        {
            return;
        }
        
        if (_iZClass != _zombieManager.GetZClassFromMenu(_player.PlayerID) && !IsNemesis)
        {
            _iZClass.Abilities.ForEach(ability => ability.UnHook());
            _iZClass = _zombieManager.GetZClassFromMenu(_player.PlayerID);
        }

        _player.SendAlert("Ваш класс => " + _iZClass.DisplayName);

        _player.SetHealth(_iZClass.Health);
        _player.SetSpeed(_iZClass.Speed);
        _player.SetGravity(_iZClass.Gravity);
        _player.SetModel(_iZClass.Model);
        _player.SwitchTeam(Team.T);

        _iZClass.Abilities.ForEach(zAbility => zAbility.SetCaster(_player));
        
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
        _iZClass.Abilities.ForEach(zAbility => zAbility.UnHook());
    }

    public IPlayer GetPlayer()
    {
        return _player;
    }

    public IZClass GetZombieClass()
    {
        return _iZClass;
    }
}