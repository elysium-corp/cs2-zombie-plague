namespace CustomEquipment.Api.Exceptions;

public sealed class NotAttachedWeaponException : NotAttachedEntityException
{
    public NotAttachedWeaponException(string? value) : base(value) { }
        
    public NotAttachedWeaponException() { }
}