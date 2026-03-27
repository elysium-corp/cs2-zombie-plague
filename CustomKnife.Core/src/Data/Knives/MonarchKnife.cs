using CustomKnife.Data.Configs;
using CustomKnife.Data.Models;
using ZPApi.Data;

namespace CustomKnife.Data.Knifes;

internal sealed class MonarchKnife(MonarchKnifeConfig config) : IKnife
{
    public byte Index { get; set; } = config.Index;
    public string DisplayName { get; set; } = config.DisplayName;
    public string Model { get; set; } = config.Model;
    public string Description { get; set; } = config.Description;
    public float Speed { get; set; } = config.Speed;
    public KnockbackData KnockbackData { get; set; } = config.KnockbackData;
    public int Gravity { get; set; } = config.Gravity;
    public float DamageMultiplier { get; set; } = config.DamageMultiplier;
}