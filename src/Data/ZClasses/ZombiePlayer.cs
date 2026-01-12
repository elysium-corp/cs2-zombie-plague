using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.ZClasses;

public class ZombiePlayer
{
    private readonly ZombieManager _zombieManager;
    private readonly IZClass _izClass;
    private readonly IPlayer _player;
    public bool IsNemesis { get; }

    public ZombiePlayer(IPlayer player, ZombieManager zombieManager, IZClass izClass, bool isNemesis = false)
    {
        IsNemesis = isNemesis;
        _zombieManager = zombieManager;
        _izClass = izClass;
        _player = player;

        Initialize();

        player.SendAlert("Ваш класс => " + izClass.DisplayName);
    }

    public bool Infect(IPlayer target)
    {
        if (target != null && !target.IsInfected() && !target.IsLastHuman() && target.PlayerPawn.ArmorValue == 0 &&
            !_player.IsNemesis())
        {
            _zombieManager.CreateZombie(target, _player.PlayerID, target.PlayerID);
            return true;
        }

        return false;
    }

    public IZClass GetZombieClass()
    {
        return _izClass;
    }

    public void Initialize()
    {
        _player.SetHealth(_izClass.Health);
        _player.SetSpeed(_izClass.Speed);
        _player.SetGravity(_izClass.Gravity);
        _player.SetModel(_izClass.Model);

        _izClass.Abilities.ForEach(zClass => zClass.SetCaster(_player));

        _player.SwitchTeam(Team.T);

        var itemServices = _player.PlayerPawn?.ItemServices;
        if (itemServices != null)
        {
            itemServices.RemoveItems();
            itemServices.GiveItem("weapon_knife_t");
        }
    }

    public void UnHookAbilities()
    {
        _izClass.Abilities.ForEach(zClass => zClass.UnHook());
    }

    public IPlayer GetPlayer()
    {
        return _player;
    }
}