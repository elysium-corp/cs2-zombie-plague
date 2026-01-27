using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.ZClasses;

public class ZombiePlayer
{
    private readonly ZombieManager _zombieManager;
    private readonly IPlayer _player;
    private IZClass _iZClass;
    public bool IsNemesis { get; }

    public ZombiePlayer(ISwiftlyCore core, ZombieManager zombieManager, IPlayer player, IZClass izClass,
        bool isNemesis = false)
    {
        _zombieManager = zombieManager;
        _player = player;
        _iZClass = izClass;
        IsNemesis = isNemesis;

        core.Scheduler.NextWorldUpdate(Initialize);
    }

    public bool Infect(IPlayer target)
    {
        if (target.PlayerPawn == null)
        {
            return false;
        }

        if (target.IsInfected() || target.IsLastHuman() || target.PlayerPawn.ArmorValue != 0 || _player.IsNemesis())
        {
            return false;
        }

        _zombieManager.CreateZombie(target, _player);

        return true;
    }

    public void Initialize()
    {
        if (_iZClass != _zombieManager.GetZClassFromMenu(_player.PlayerID))
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

        _iZClass.Abilities.ForEach(zClass => zClass.SetCaster(_player));
        
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
        _iZClass.Abilities.ForEach(zClass => zClass.UnHook());
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