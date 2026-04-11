using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Data.Abilities.Contracts;

public interface IAbility
{
    public bool IsActive { get; set; }
    public void Use();
    public void SetCaster(IPlayer caster);
    public void Hook();
    public void UnHook();
}