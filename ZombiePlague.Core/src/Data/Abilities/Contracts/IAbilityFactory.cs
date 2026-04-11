namespace ZombiePlague.Core.Data.Abilities.Contracts;

internal interface IAbilityFactory
{
    public IAbility Create<T>() where T : IAbility;
}