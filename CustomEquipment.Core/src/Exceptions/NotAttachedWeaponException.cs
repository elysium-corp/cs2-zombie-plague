namespace CustomEquipment.Exceptions;

internal sealed class NotAttachedWeaponException : SystemException
{
    public NotAttachedWeaponException(string? value) : base(value) { }
        
    public NotAttachedWeaponException() { }
}