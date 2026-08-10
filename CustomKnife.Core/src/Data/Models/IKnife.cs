using ZombiePlague.Api.Data;

namespace CustomKnife.Data.Models;

public interface IKnife
{
    bool Enabled { get; }

    string InternalName { get; }

    string DisplayName { get; }

    string Model { get; }

    string Description { get; }

    float Speed { get; }

    KnockbackData KnockbackData { get; }

    int Gravity { get; }

    float DamageMultiplier { get; }
}