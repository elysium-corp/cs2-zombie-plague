using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Zombies.Controller;
using ZombiePlague.Core.Data.Zombies.ZClasses;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Zombies;

internal sealed class Zombie : IZombie
{
    public IPlayer Player { get; }
    public IZClass ZClass { get; }
    public bool IsNemesis { get; }
    public ISoundController? SoundController { get; private set; }

    private readonly ISwiftlyCore _core;

    public Zombie(ISwiftlyCore core, IPlayer player, IZClass zClass,
        bool isNemesis = false)
    {
        Player = player;
        ZClass = zClass;
        IsNemesis = isNemesis;
        _core = core;
    }

    public void Initialize()
    {
        if (!Player.IsAlive)
        {
            return;
        }
        
        SetProperties();

        ZClass.Abilities.ForEach(zAbility => zAbility.SetCaster(Player));
        SoundController?.Dispose();
        SoundController = new ZombieSoundController(_core, this);

        Player.SendAlert("Ваш класс => " + ZClass.DisplayName);
    }

    public void Dispose()
    {
        ZClass.Abilities.ForEach(zAbility => zAbility.UnHook());
        SoundController?.Dispose();
        SoundController = null;
    }

    private void SetProperties()
    {
        Player.SetHealth(ZClass.Health);
        Player.SetSpeed(ZClass.Speed);
        Player.SetGravity(ZClass.Gravity);
        Player.SetModel(_core, ZClass.Model);
        Player.SwitchTeam(Team.T);
        
        var itemServices = Player.PlayerPawn?.ItemServices;
        if (itemServices == null)
        {
            return;
        }

        itemServices.RemoveItems();
        itemServices.GiveItem("weapon_knife_t");
    }

}
