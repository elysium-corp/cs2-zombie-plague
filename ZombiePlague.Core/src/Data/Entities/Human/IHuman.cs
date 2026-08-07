namespace ZombiePlague.Core.Data.Entities.Human;

internal interface IHuman : IPlayerRole
{
    public IHClass HClass { get; }
}