using ZombiePlague.Api.Data;

namespace CustomKnife.Data.Models;

public interface IKnife
{
    public byte Index { get; set; }
    
    public string DisplayName { get; set; }
    
    public string Model { get; set; }
    
    public string Description { get; set; }
    
    public float Speed { get; set; }
    
    public KnockbackData KnockbackData { get; set; }
    
    public int Gravity { get; set; }
    
    public float DamageMultiplier { get; set; }
}