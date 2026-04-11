namespace ZombiePlague.Core.Data.Abilities.Contracts;

internal interface ICooldownRestricted
{
    bool IsActive { get; set; }
    
    float Cooldown { get; }

    void StartCooldown();

    bool ShouldResetCooldown();

    bool IsCooldownNotify { get; }
    
    void ResetCooldown();
}