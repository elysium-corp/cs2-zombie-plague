namespace CS2ZombiePlague.Data.Abilities.Contracts;

public interface ICooldownRestricted
{
    bool IsActive { get; set; }
    
    float Cooldown { get; }

    void StartCooldown();

    bool ShouldResetCooldown();

    bool IsCooldownNotify { get; }
    
    void ResetCooldown();
}