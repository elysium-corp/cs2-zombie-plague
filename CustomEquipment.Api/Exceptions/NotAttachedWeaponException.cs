namespace CustomEquipment.Api.Exceptions;

public sealed class NotAttachedWeaponException : KeyNotFoundException
{
    public NotAttachedWeaponException(string? value) : base(value) { }
        
    public NotAttachedWeaponException() { }
}