using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Abilities.Contracts;

internal interface IAbility
{
    public bool IsActive { get; set; }
    public void Use();
    public void SetCaster(IPlayer caster);
    public void Hook();
    public void UnHook();
}