using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Zombies.Controller;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Entities.Zombie;

internal sealed class Zombie : IZombie
{
    public IZClass ZClass { get; }
    
    public IPlayer Owner { get; }
    
    private ISoundController? SoundController { get; set; }
    
    private readonly ISwiftlyCore _core;

    private bool _isBindScheduled;
    
    private Zombie(ISwiftlyCore core, IPlayer owner, IZClass zClass)
    {
        _core = core;
        Owner = owner;
        ZClass = zClass;
    }

    public void Bind()
    {
        if (_isBindScheduled) return;
        
        _isBindScheduled = true;
        _core.Scheduler.NextWorldUpdate(InternalBind);
    }

    public void Unbind()
    {
        _isBindScheduled = false;
        
        foreach (var ability in ZClass.Abilities)
        {
            ability.UnHook();
        }
        
        SoundController?.Dispose();
        SoundController = null;
    }

    private void InternalBind()
    {
        if (!_isBindScheduled) return;

        if (
            !Owner.IsValid ||
            !Owner.IsAlive ||
            Owner.PlayerPawn is not { IsValid: true, ItemServices: { } itemServices } pawn
        )
        {
            _isBindScheduled = false;
            return;
        }
        
        Owner.SetHealth(ZClass.Health);
        Owner.SetSpeed(ZClass.Speed);
        Owner.SetGravity(ZClass.Gravity);
        
        if (!string.IsNullOrWhiteSpace(ZClass.Model))
        {
            pawn.SetModel(ZClass.Model);
        }
        
        itemServices.RemoveItems();
        itemServices.GiveItem(ZombieKnife);
        
        foreach (var ability in ZClass.Abilities)
        {
            ability.SetCaster(Owner);
        }

        SoundController = new ZombieSoundController(_core, this);
    }

    public static IZombie Create(ISwiftlyCore core, IPlayer player, IZClass zClass)
    {
       return new Zombie(core, player, zClass);
    }

    private const string ZombieKnife = "weapon_knife_t";
}