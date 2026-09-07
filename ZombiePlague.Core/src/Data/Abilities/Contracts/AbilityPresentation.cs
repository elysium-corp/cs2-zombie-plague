namespace ZombiePlague.Core.Data.Abilities.Contracts;

internal sealed record AbilityPresentation(string Key, string NameKey, string Kind);

internal interface IPresentedAbility
{
    AbilityPresentation? Presentation { get; set; }
}
