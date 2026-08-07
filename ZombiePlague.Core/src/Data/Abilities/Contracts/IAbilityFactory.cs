namespace ZombiePlague.Core.Data.Abilities.Contracts;

internal interface IAbilityFactory
{
    public IAbility Create<T>() where T : IAbility;

    public IAbility CreateByName(string abilityName);

    public List<IAbility> CreateFromStrings(List<string> abilities);
}