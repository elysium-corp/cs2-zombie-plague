namespace CustomEquipment.Api.Enums;

[Flags]
public enum AccessFlags
{
    None = 0,

    Human = 1 << 0,

    Zombie = 1 << 1,

    All = Human | Zombie
}