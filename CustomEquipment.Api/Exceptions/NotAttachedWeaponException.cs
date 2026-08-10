namespace CustomEquipment.Api.Exceptions;

public sealed class NotAttachedWeaponException : SystemException
{
    public NotAttachedWeaponException(string? value) : base(value) { }
        
    public NotAttachedWeaponException() { }
}