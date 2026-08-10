using CustomEquipment.Data.Equipments.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Data.Equipments.Contracts;

public abstract class BaseEquipment : BaseItem, IEquipment
{
    public abstract string InheritorName { get; }

    public override string InternalName => ToInternalName(DisplayName);

    public abstract WeaponType WeaponType { get; }

    public virtual bool TryPurchase(IPlayer owner)
    {
        ArgumentNullException.ThrowIfNull(owner);

        if (!owner.IsValid || !owner.IsAlive)
        {
            return false;
        }

        OnPurchase(owner);
        return true;
    }

    public abstract void OnPurchase(IPlayer owner);

    private static string ToInternalName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        return name.ToLowerInvariant().Replace(" ", "_");
    }
}
