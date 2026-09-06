namespace ZombiePlague.Core.Data.Abilities.Contracts;

internal sealed record AbilityPresentation(string Key, string Name, string Kind);

internal interface IPresentedAbility
{
    AbilityPresentation? Presentation { get; set; }
}
