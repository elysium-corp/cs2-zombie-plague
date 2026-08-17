using CustomKnife.Data.Configs;
using CustomKnife.Data.Models;
using ZombiePlague.Api.Data;

namespace CustomKnife.Data.Knives;

internal sealed class Axe(AxeConfig config) : IKnife
{
    public bool Enabled { get; } = config.Enabled;
    
    public string InternalName { get; } = config.InternalName;
    
    public string DisplayName { get; } = config.DisplayName;
    
    public string Model { get; } = config.Model;
    
    public string Description { get; } = config.Description;
    
    public float Speed { get; } = config.Speed;
    
    public KnockbackData KnockbackData { get; } = config.KnockbackData;
    
    public int Gravity { get; } = config.Gravity;
    
    public float DamageMultiplier { get; } = config.DamageMultiplier;
}