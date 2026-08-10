using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Entities.Human;

internal sealed class Human : IHuman
{
    public IPlayer Owner { get; }

    public IHClass HClass { get; }

    private readonly ISwiftlyCore _core;

    private bool _isBindScheduled;

    private Human(ISwiftlyCore core, IPlayer owner, IHClass hClass)
    {
        _core = core;
        Owner = owner;
        HClass = hClass;
    }

    public void Bind()
    {
        if (_isBindScheduled) return;
        
        _isBindScheduled = true;
        _core.Scheduler.NextWorldUpdate(InternalBind);
        
        var ability1 = new DoubleJump(_core, new DoubleJumpConfig());
        ability1.SetCaster(Owner);
        ability1.Hook();
    }

    public void Unbind()
    {
        _isBindScheduled = false;

        foreach (var ability in HClass.Abilities)
        {
            ability.UnHook();
        }
    }

    private void InternalBind()
    {
        if (!_isBindScheduled) return;

        if (
            !Owner.IsValid ||
            !Owner.IsAlive ||
            Owner.PlayerPawn is not { IsValid: true } pawn
        )
        {
            _isBindScheduled = false;
            return;
        }

        Owner.SetHealth(HClass.Health);
        Owner.SetSpeed(HClass.Speed);
        Owner.SetGravity(HClass.Gravity);
        Owner.SetArmor(HClass.Armor);

        if (!string.IsNullOrWhiteSpace(HClass.Model))
        {
            pawn.SetModel(HClass.Model);
        }

        foreach (var ability in HClass.Abilities)
        {
            ability.SetCaster(Owner);
        }
    }

    public static IHuman Create(ISwiftlyCore core, IPlayer player, IHClass hClass)
    {
        return new Human(core, player, hClass);
    }
}