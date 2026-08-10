using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Data;

public abstract class BaseEquipment : BaseItem, IEquipment
{
    public abstract string InheritorName { get; }

    public override string InternalName => ToInternalName(DisplayName);

    public abstract WeaponType WeaponType { get; }

    public abstract void OnPurchase(IPlayer owner);

    private static string ToInternalName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        return name.ToLowerInvariant().Replace(" ", "_");
    }
}